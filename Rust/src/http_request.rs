
// HTTP request generator

use url::{Url, ParseError, Position}; // https://docs.rs/url/1.2.3/url/
use std::collections::BTreeMap;

pub struct HttpRequest {
    url: Url,
    headers: BTreeMap<String, Vec<String>>,
}

#[derive(Debug, PartialEq)]
pub enum HttpTarget {
    Unsecure (String),
    Secure (String),
    NoTarget
}

impl HttpRequest {
    /// Create a new request with a complete URL
    pub fn new(target_url: &str) -> Result<HttpRequest, ParseError> {
        Ok(HttpRequest {
            url: try!(Url::parse(target_url)),
            headers: BTreeMap::new(),
        })
    }

    /// Add a new header value to the request. Multiple calls with the same header key
    /// will result in comma concatenated values.
    pub fn add_header(&mut self, header: &str, value: &str) {
        let header = header.to_owned();
        self.headers.entry(header).or_insert(vec![]).push(value.to_owned());
    }

    /// Returns a secure/unsecure discriminated string that has the target
    /// domain complete with target port. If no port was given, one will be guessed
    /// based on the request scheme.
    pub fn request_target(& self) -> HttpTarget {
        let mut conn = String::new();
        conn.push_str(self.url.host_str().unwrap_or("localhost"));
        conn.push(':');
        conn.push_str(&(self.url.port_or_known_default().unwrap_or(80).to_string()));
        return match self.url.scheme() {
            "http"  => HttpTarget::Unsecure(conn),
            "https" => HttpTarget::Secure(conn),
            _       => HttpTarget::NoTarget
        };
    }

    /// return the target domain. This is used to check TLS certificates.
    pub fn domain(& self) -> String {
        self.url.host_str().unwrap_or("localhost").to_owned()
    }

    /// Return the utf-8 bytes that form the get line, headers, and header terminator orf tihs
    /// request.
    pub fn request_bytes(& self, verb: &str, content_length: Option<u64>) -> Vec<u8> {
        let mut req = String::new();

        // primary line
        req.push_str(verb);
        req.push(' ');
        req.push_str(&(self.url[Position::BeforePath..]));
        req.push_str(" HTTP/1.1\r\n");

        // Mandatory 'Host' header:
        req.push_str("Host: ");
        req.push_str(self.url.host_str().unwrap());
        req.push_str("\r\n");

        // Optional connection header. This simple client only supports 'close' at the moment.
        req.push_str("Connection: close\r\n");

        // Not entirely mandatory Content-Length header:
        //    https://www.w3.org/Protocols/rfc2616/rfc2616-sec4.html#sec4.4
        req.push_str("Content-Length: ");
        req.push_str(&(content_length.unwrap_or(0).to_string()));
        req.push_str("\r\n");

        // Headers
        for (key, values) in &self.headers {
            req.push_str(&key);
            req.push_str(": ");
            req.push_str(&(values.join(",")));
            req.push_str("\r\n");
        }

        req.push_str("\r\n");
        let res = req.into_bytes().to_owned();

        return res;
    }
}
