
extern crate shift_it;
//use shift_it::raw_call;

static SAMPLE_REQUEST: &'static str =
r#"GET http://www.purple.com/ HTTP/1.1
Host: www.purple.com
Accept: text/html

"#;


#[test]
fn very_simple_http_call() {
    let rq = shift_it::http_request::HttpRequest::new("www.purple.com:80");

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
