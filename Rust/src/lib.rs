// a quick test of HTTP over a plain TCP socket.

extern crate url;
extern crate native_tls;

use native_tls::TlsConnector;

use std::net::{TcpStream, Shutdown};
use std::io::{Read, Write, Error};
use std::time::Duration;

pub mod http_request;

pub fn raw_call(target: &str, request: &str) -> Result<String, Error> {
    let mut stream = TcpStream::connect(target).unwrap();

    stream.set_read_timeout(Some(Duration::from_millis(500))).unwrap();
    stream.set_write_timeout(Some(Duration::from_millis(500))).unwrap();

    try!(stream.write(&(request.to_string().into_bytes())));
    try!(stream.flush());


    let mut result = vec![];
    let mut buf = &mut[0u8;1024];
    while let Ok(len) = stream.read(buf) {
        if len < 1 {break;}
        result.extend(buf[0..len].iter().cloned());
    }
    //stream.read_to_end(&mut result).unwrap(); // this doesn't behave well.
    try!(stream.shutdown(Shutdown::Both));

    // the `into_owned` here is critical for the return lifetime
    let result_str = String::from_utf8_lossy(&result).into_owned();
    return Ok(result_str);
}

pub fn raw_tls(target: &str, request: &str) -> Result<String, Error> {
    let connector = TlsConnector::builder().unwrap().build().unwrap();

    let stream = TcpStream::connect("www.google.co.uk:443").unwrap();
    stream.set_read_timeout(Some(Duration::from_millis(500))).unwrap();
    stream.set_write_timeout(Some(Duration::from_millis(500))).unwrap();

    let mut stream = connector.connect("www.google.co.uk", stream).unwrap();

    //stream.write_all(&(SAMPLE_REQUEST.to_string().into_bytes())).unwrap();
    //                    ^^^ the wrong newlines are hurting...
    stream.write_all(b"GET / HTTP/1.0\r\nHost: www.google.co.uk\r\nAccept: text/html\r\n\r\n").unwrap();
    let mut res = vec![];
    stream.read_to_end(&mut res).unwrap();

    let result_str = String::from_utf8_lossy(&res).into_owned();
    return Ok(result_str);
}

