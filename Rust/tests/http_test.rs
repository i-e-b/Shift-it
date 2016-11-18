
extern crate shift_it;
//use shift_it::raw_call;

static SAMPLE_REQUEST: &'static str =
r#"GET http://www.purple.com/ HTTP/1.1
Host: www.purple.com
Accept: text/html

"#;

#[test]
fn request_bytes() {
    let mut rq = shift_it::http_request::HttpRequest::new("www.purple.com:80");
    rq.add_header("Accept", "text/html");

    let rq_str = String::from_utf8(rq.request_bytes()).expect("could not decode string bytes");

    assert_eq!(rq_str, "GET / HTTP/1.1\r\nHost: www.purple.com:80\r\nAccept: text/html\r\n\r\n");
}

// The following tests always fail. They are just sketches
/*
#[test]
fn very_simple_http_call() {
    let result = shift_it::raw_call("www.purple.com:80", SAMPLE_REQUEST);
    match result {
        Ok(body) => assert_eq!(body, "test"),
        Err(e) => panic!(e)
    };

}

#[test]
fn very_simple_https_call() {
    let result = shift_it::raw_tls("google.com:443", SAMPLE_REQUEST);
    match result {
        Ok(body) => assert_eq!(body, "test"),
        Err(e) => panic!(e)
    };
}
*/
