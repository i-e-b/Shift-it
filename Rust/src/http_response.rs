
// HTTP Response parser

use std::collections::BTreeMap;
use std::rc::Rc;
use std::cell::{RefCell, RefMut};
use std::io::{Read, Error, ErrorKind};
use std::io;
use std::fmt;

/// Response received from a completed HTTP call
pub struct HttpResponse<'a> {
    pub status_code: u16,
    pub status_class: StatusClass,
    pub status_message: String,
    pub headers: BTreeMap<String, Vec<String>>,

    // The underlying reader, plus some buffers for decoding
    body: Rc<RefCell<Read + 'a>>,
    readBuf: Vec<u8>,
    chunked: bool // TODO: Replace by adjustable wrapping after headers are read?
}

#[derive(Debug)]
struct HttpStatus {
    code: u16,
    class: StatusClass,
    message: String,
}

/// General state of the HTTP status (for use with `match` blocks)
#[derive(Debug, PartialEq)]
pub enum StatusClass {
    Invalid, Information, Success, Redirection, ClientError, ServerError
}

impl<'a> fmt::Debug for HttpResponse<'a> {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
        write!(f, "HTTP {} {}\r\n{:?}\r\n<Body not shown in debug>", self.status_code, self.status_message, self.headers)
    }
}

impl<'a> HttpResponse<'a> {
    /// Wrap a read stream in a HTTP decoder.
    /// Needs the stream to be inside a counted ref cell, so
    /// pass like `Rc::new(RefCell::new(stream))`
    pub fn new<R: 'a + Read>(response_ref: Rc<RefCell<R>>) -> Result<HttpResponse<'a>, Error> {
        let mut response_stream: RefMut<R> = response_ref.borrow_mut();

        let status = try!(read_status_line(next_line(&mut *response_stream)));

        // read headers until we get an empty line
        let mut headers:BTreeMap<String, Vec<String>> = BTreeMap::new();
        loop {
            let line = next_line(&mut *response_stream);
            if line == "" { break; }
            try!(read_header(&mut headers, line));
            // TODO: gather all the headers we can and then return? Would allow partial recovery.
        }

        // Transfer encoding flags:
        let chunked = header_match_any_case(&headers, "Transfer-Encoding", "chunked");

        let result = HttpResponse {
            status_code: status.code,
            status_class: status.class,
            status_message: status.message,
            headers: headers,
            body: response_ref.clone(),
            readBuf: Vec::with_capacity(10240),
            chunked: chunked
        };

        return Ok(result);
    }

}

/// Iterate over the bytes of the response body
impl<'a> Iterator for HttpResponse<'a> {
    type Item = u8;

    /// Next byte of the body. Returns `None` when complete.
    fn next(&mut self) -> Option<u8> {
        let mut one_buf = [0];
        match self.read(&mut one_buf) { // use our own Read implementation, to get all the unwrapping
            Err(_) => None,
            Ok(len) => if len == 1 {Some(one_buf[0])} else {None}
        }
    }
}

/// Access to the underlying reader without needing to unpack the `Rc` yourself.
impl<'a> Read for HttpResponse<'a> {
    fn read(&mut self, mut buf: &mut [u8]) -> io::Result<usize> {
        // TODO: Need to add decode for compressed and/or chunked responses here.
        // Ideally, we can make a wrapper implementation that owns the original stream reader.

        let local_body = self.body.clone();
        let mut response_stream: RefMut<Read + 'a> = local_body.borrow_mut();

        if self.chunked {
            let len = try!(read_chunk_length(*response_stream));
            // read the length header
            // if zero, stream over, EOF
            // else read that many into the buffer and trim end
        }
        return (*response_stream).read(&mut buf);
    }

}

fn read_chunk_length<R: Read>(mut stream: &mut R) {
    let mut one_buf = [0];
    let mut len_str = String::new();
    loop {

    }
}

/// Checks the given key to see if any of the values match the given one, ignoring case
fn header_match_any_case(headers: &BTreeMap<String, Vec<String>>, header_key: &str, expected_value: &str) -> bool {
    let target = expected_value.to_lowercase();
    return headers.get(header_key).unwrap_or(&vec![]).iter().any(|ref v| v.to_lowercase() == target);
}

/// Fill an existing header map with the values of a single header line
fn read_header(headers: &mut BTreeMap<String, Vec<String>>, header_line: String) -> Result<(), Error> {
    let colon = header_line.find(": ");
    if colon == None {
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

/// Return a status object for a HTTP response's first line
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
/// Attempts to handle variable line endings (even though the HTTP
/// spec insists on `\r\n`, many servers are non compliant)
fn next_line<R: Read>(mut stream: &mut R) -> String {
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

