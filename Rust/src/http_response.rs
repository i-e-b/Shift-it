
// HTTP Response parser

use std::collections::BTreeMap;
use std::io;
use std::io::{Read, Error, ErrorKind};
use std::fmt;

pub struct HttpResponse<'a> {
    status_code: u16,
    status_class: StatusClass,
    status_message: String,
    headers: BTreeMap<String, Vec<String>>,
    body: Box<Read + 'a>
}

#[derive(Debug)]
struct HttpStatus {
    code: u16,
    class: StatusClass,
    message: String,
}

#[derive(Debug)]
pub enum StatusClass {
    Invalid, Information, Success, Redirection, ClientError, ServerError
}

impl<'a> fmt::Debug for HttpResponse<'a> {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
        write!(f, "HTTP {} {}\r\n{:?}\r\n<Body not shown in debug>", self.status_code, self.status_message, self.headers)
    }
}

impl<'a> HttpResponse<'a> {
    pub fn new<R: Read>(mut response_stream: &'a mut R) -> Result<HttpResponse<'a>, Error> {
        let status = try!(read_status_line(next_line(&mut response_stream)));

        // read headers until we get an empty line
        let mut headers:BTreeMap<String, Vec<String>> = BTreeMap::new();
        loop {
            let line = next_line(&mut response_stream);
            if line == "" { break; }
            try!(read_header(&mut headers, line));
        }

        let result = HttpResponse {
            status_code: status.code,
            status_class: status.class,
            status_message: status.message,
            headers: headers,
            body: Box::new(response_stream)
        };

        return Ok(result);
    }

}

fn read_header(headers: &mut BTreeMap<String, Vec<String>>, header_line: String) -> Result<(), Error> {
    let colon = header_line.find(": ");
    if colon == None { // TODO: gather all the headers we can and then return? Would allow partial recovery.
        return Err(Error::new(ErrorKind::InvalidData, "invalid header line"));
    }
    let colon = colon.unwrap();

    let (name, values) = header_line.split_at(colon); // "head: value, value" -> "head", ": value, value"
    let mut value_vec =
        (values[2..]).split(',')
        .map(|p|{p.to_string().trim().to_string()}).collect(); // "value, value" -> ["value","value"]

    headers.entry(name.to_string()).or_insert(vec![]).append(&mut value_vec);

    return Ok(());
}

fn read_status_line(status_line: String) -> Result<HttpStatus, Error> {
    let mut status = HttpStatus {
        code: 0,
        class: StatusClass::Invalid,
        message: String::new(),
    };

    let mut parts = status_line.splitn(3, ' ');
    if let Some(http_version) = parts.next() {
        if http_version != "HTTP/1.1" {
            return Err(Error::new(ErrorKind::Other, "unexpected HTTP version returned"));
        }
    }
    if let Some(status_string) = parts.next() {
        status.code = status_string.parse::<u16>().unwrap_or(0);
        status.class = match status.code {
            100...199 => StatusClass::Information,
            200...299 => StatusClass::Success,
            300...399 => StatusClass::Redirection,
            400...499 => StatusClass::ClientError,
            500...599 => StatusClass::ServerError,
            _     => StatusClass::Invalid
        };
    };
    if let Some(status_message) = parts.next() {
        status.message = status_message.to_owned();
    }
    return Ok(status);
}

/// read the next line string from a byte stream.
fn next_line<R: Read>(stream: &mut R) -> String {
    let mut sb = String::new();
    let mut buf: Vec<u8> = vec![0];
    let mut s = 0u8;

    loop {
        if let Err(_) = stream.read_exact(&mut buf) { break; }
        let b = buf[0];
        if b == b'\r' || b == b'\n' {
            if s == 2 { break; }
            if s == 1 && b != b'\n' { break; }
            s += 1;
        } else {
            s = 2;
            sb.push(b as char);
        }
    }

    return sb;
}
