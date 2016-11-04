// a quick test of HTTP over a plain TCP socket.

use std::net::{TcpStream, Shutdown};
use std::io::{Read, Write, Error};
use std::time::Duration;

static SAMPLE_REQUEST: &'static str =
r#"GET http://www.purple.com/ HTTP/1.1
Host: www.purple.com
Accept: text/html

"#;

pub fn raw_call(target: &str, request: &str) -> Result<String, Error> {
    let mut result: Vec<u8> = Vec::new();
    let mut stream = TcpStream::connect(target).unwrap();

    stream.set_read_timeout(Some(Duration::from_millis(500))).unwrap();
    stream.set_write_timeout(Some(Duration::from_millis(500))).unwrap();

    try!(stream.write(&(request.to_string().into_bytes())));
    try!(stream.flush());

    let mut buf = &mut[0u8;1024];
    while let Ok(len) = stream.read(buf) {
        if len < 1 {break;}
        result.extend(buf[0..len].iter().cloned());
    }

    try!(stream.shutdown(Shutdown::Both));

    let result_str = String::from_utf8_lossy(&result).into_owned(); // the `into_owned` here is critical
    // for the return lifetime

    return Ok(result_str);
}

pub fn example() {
    let mut result: Vec<u8> = Vec::new();
    println!("Connecting");
    let mut stream = TcpStream::connect("www.purple.com:80").unwrap();
    // This is equivalent to `TcpStream::connect("153.104.63.227:80")` due to the
    // std::net::ToSocketAddrs trait.

    stream.set_read_timeout(Some(Duration::from_millis(500))).unwrap();
    stream.set_write_timeout(Some(Duration::from_millis(500))).unwrap();
    // stream.set_nonblocking(false).unwrap(); // The socket is blocking by default.

    println!("Writing request");
    if let Err(e) = stream.write(&(SAMPLE_REQUEST.to_string().into_bytes())) {
        println!("Failed to write socket:{:?}", e);
        return;
    }
    println!("Flushing");
    stream.flush().expect("Could not flush tcp stream");

    print!("Reading response");
    let mut buf = &mut[0u8;1024];
    while let Ok(len) = stream.read(buf) {
        if len < 1 {break;}
        print!(".");
        result.extend(buf[0..len].iter().cloned()); // Does the equivalent of `result.push(&buf[0..len]);`
        // but copies the values from the buffer into
        // the result vector.
    }
    println!("");

    let result_str = String::from_utf8_lossy(&result);
    println!("{}", &result_str);

    stream.shutdown(Shutdown::Both).expect("Could not close connection");
}
