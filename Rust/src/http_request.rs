

use std::collections::BTreeMap;

pub struct HttpRequest {
    //    verb: String,
    //    url: String,
    headers: BTreeMap<String, Vec<String>>,
}

impl HttpRequest {
    pub fn new() -> HttpRequest {
        HttpRequest {
            headers: BTreeMap::new(),
        }
    }

    pub fn get(url: &str) {
    }

    pub fn request_bytes(& self) -> Vec<u8> {
        let req = String::new();

        for (key, values) in &self.headers {
            println!("{}: {}\r\n", key, values.join(", "));
        }

        let res = req.into_bytes().to_owned();

        return res;
    }
}
