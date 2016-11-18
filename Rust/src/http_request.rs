

use std::collections::BTreeMap;

pub struct HttpRequest {
    verb: String,
    url: String,
    headers: BTreeMap<String, Vec<String>>,
}

impl HttpRequest {
    pub fn new(target_url: &str) -> HttpRequest {
        HttpRequest {
            verb: "GET".to_string(),
            url: target_url.to_string(),
            headers: BTreeMap::new(),
        }
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
        //req.push_str(&self.url); // TODO: url parsing
        req.push('/');
        req.push_str(" HTTP/1.1\r\n");

        // Mandatory 'Host' header:
        req.push_str("Host: ");
        req.push_str(&self.url);
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
