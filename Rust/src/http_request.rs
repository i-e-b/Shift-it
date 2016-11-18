

use url::{Url, ParseError, Position}; // https://docs.rs/url/1.2.3/url/
use std::collections::BTreeMap;
use std::io;

pub struct HttpRequest {
    verb: String,
    url: Url,
    headers: BTreeMap<String, Vec<String>>,
}

impl HttpRequest {
    pub fn new(target_url: &str) -> Result<HttpRequest, ParseError> {
        Ok(HttpRequest {
            verb: "GET".to_string(),
            url: try!(Url::parse(target_url)),
            headers: BTreeMap::new(),
        })
    }

    pub fn add_header(&mut self, header: &str, value: &str) {
        let header = header.to_owned();

        self.headers.entry(header).or_insert(vec![]).push(value.to_owned());
    }

    pub fn request_bytes(& self) -> Vec<u8> {
        let mut req = String::new();

        // primary line
        req.push_str(&self.verb);
        req.push(' ');
        req.push_str(&(self.url[Position::BeforePath..]));
        req.push_str(" HTTP/1.1\r\n");

        // Mandatory 'Host' header:
        req.push_str("Host: ");
        req.push_str(self.url.host_str().unwrap());
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
