// a quick test of HTTP over a plain TCP socket.

extern crate url;
extern crate native_tls;

use native_tls::TlsConnector;

use std::net::{TcpStream};
use std::io;
use std::io::{Read, Write, Error, ErrorKind};
use std::time::Duration;

pub mod http_request;
pub mod http_response;

use self::http_request::{HttpRequest, HttpTarget};
use self::http_response::{HttpResponse};

pub fn call_no_data<'a>(rq: HttpRequest) -> Result<HttpResponse<'a>, Error> { call(rq, io::empty()) }

pub fn call<'a, R: Read>(rq: HttpRequest, body_stream: R) -> Result<HttpResponse<'a>, Error> {
    let domain = rq.domain();
    let target = rq.request_target();
    let request = rq.request_bytes("GET", None);

    return match target {
        HttpTarget::Secure(target)   => raw_tls(&target, &domain, request, body_stream),
        HttpTarget::Unsecure(target) => raw_call(&target, request, body_stream),
        HttpTarget::NoTarget         => Err(Error::new(ErrorKind::Other, "no valid target in uri"))
    };
}

/// target is like `www.purple.com:80`. Request is the http request string.
fn raw_call<'a, R: Read>(target: &str, request: Vec<u8>, mut body: R) -> Result<HttpResponse<'a>, Error> {
    // something like this?
    let stream: &'a = TcpStream::connect(target));
    let stream: &'a = try!(stream);
    let mut stream = stream;

    let wha = try!(TcpStream::connect(target));
    let mut stream: &'a TcpStream = &mut wha;

    stream.set_read_timeout(Some(Duration::from_millis(500))).unwrap();
    stream.set_write_timeout(Some(Duration::from_millis(500))).unwrap();

    try!(stream.write(&request));
    try!(io::copy(&mut body, &mut stream));
    try!(stream.flush());

    let response = try!(HttpResponse::new(&mut stream));
    return Ok(response);
}

/// target is like `www.google.com:443`. Domain is needed to match the cert, like `www.google.com`.
/// Request is the http request string.
fn raw_tls<'a, R: Read>(target: &str, domain: &str, request: Vec<u8>, mut body: R) -> Result<HttpResponse<'a>, Error> {
    let connector = TlsConnector::builder().unwrap().build().unwrap();

    let stream = TcpStream::connect(target).unwrap();
    stream.set_read_timeout(Some(Duration::from_millis(1500))).unwrap();
    stream.set_write_timeout(Some(Duration::from_millis(1500))).unwrap();

    let mut stream = connector.connect(domain, stream).expect("Failed to connect");

    try!(stream.write_all(&request));
    try!(io::copy(&mut body, &mut stream));
    try!(stream.flush());

    let response = try!(HttpResponse::new(&mut stream));
    return Ok(response);
}

