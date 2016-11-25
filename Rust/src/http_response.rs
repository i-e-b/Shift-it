
// HTTP Response parser

use std::collections::BTreeMap;
use std::io;
use std::io::{Read, Write, Error, ErrorKind};


pub struct HttpResponse {
    status_code: u16,
    status_class: StatusClass,
    status_message: String,
    headers: BTreeMap<String, Vec<String>>,
}

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

        return Ok(());
    }

    fn read_status_line(&mut self, status_line: String) -> Result<(), Error> {
        let mut parts = status_line.split(' ');
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
                _        => StatusClass::Invalid
            };
        };
        //TODO:  self.status_message = parts.join(" ");
        return Ok(());
    }
}


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