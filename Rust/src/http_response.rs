
// HTTP Response parser

use std::collections::BTreeMap;
use std::io;
use std::io::{Read, Write, Error, ErrorKind};
use std::str::Split;


#[derive(Debug)]
pub struct HttpResponse {
    status_code: u16,
    status_class: StatusClass,
    status_message: String,
    headers: BTreeMap<String, Vec<String>>,
}

#[derive(Debug)]
pub enum StatusClass {
    Invalid, Information, Success, Redirection, ClientError, ServerError
}

impl HttpResponse {
    pub fn new() -> HttpResponse {
        HttpResponse {
            status_code: 0,
            status_class: StatusClass::Invalid,
            status_message: String::new(),
            headers: BTreeMap::new(),
        }
    }

    pub fn read<R: Read>(&mut self, mut response_stream: &mut R) -> Result<(), Error> {
        try!(self.read_status_line(next_line(&mut response_stream)));

        // read headers until we get an empty line
        loop {
            let line = next_line(&mut response_stream);
            if line == "" { break; }
            try!(self.read_header(line));
        }

        return Ok(());
    }

    fn read_header(&mut self, header_line: String) -> Result<(), Error> {
        let colon = header_line.find(": ");
        if colon == None { // TODO: gather all the headers we can and then return? Would allow partial recovery.
            return Err(Error::new(ErrorKind::InvalidData, "invalid header line"));
        }
        let colon = colon.unwrap();

        let (name, values) = header_line.split_at(colon); // "head: value, value" -> "head", ": value, value"
        let mut value_vec =
            (values[2..]).split(',')
            .map(|p|{p.to_string().trim().to_string()}).collect(); // "value, value" -> ["value","value"]

        self.headers.entry(name.to_string()).or_insert(vec![]).append(&mut value_vec);

        return Ok(());
    }

    fn read_status_line(&mut self, status_line: String) -> Result<(), Error> {
        let mut parts = status_line.splitn(3, ' ');
        if let Some(http_version) = parts.next() {
            if http_version != "HTTP/1.1" {
                return Err(Error::new(ErrorKind::Other, "unexpected HTTP version returned"));
            }
        }
        if let Some(status_string) = parts.next() {
            self.status_code = status_string.parse::<u16>().unwrap_or(0);
            self.status_class = match self.status_code {
                100...199 => StatusClass::Information,
                200...299 => StatusClass::Success,
                300...399 => StatusClass::Redirection,
                400...499 => StatusClass::ClientError,
                500...599 => StatusClass::ServerError,
                    _     => StatusClass::Invalid
            };
        };
        if let Some(status_message) = parts.next() {
            self.status_message = status_message.to_owned();
        }
        return Ok(());
    }
}

/*
/// Take a split string and return it as a single space-joined string.
fn rejoin_split(s: Split<char>) -> String {
    let mut string =
        s.map(String::from)
        .fold(String::new(), |acc, p| {
            acc + " " + &p
        });
    return string.trim().to_string();
}*/

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
