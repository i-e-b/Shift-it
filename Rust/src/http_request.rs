

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

    pub fn add_header(& self, header: &str, value: &str) {
        match self.headers.get(header) {
            Some(list) => {
                // add to the list
            },
            None => {
                // add a new list to the hashmap
            }
        }
    }

    pub fn request_bytes(& self) -> Vec<u8> {
        let mut req = String::new();

        // primary line
        req.push_str(&self.verb);
        req.push(' ');
        req.push_str(&self.url);
        req.push_str(" HTTP/1.1\r\n");

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
