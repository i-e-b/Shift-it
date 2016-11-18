
extern crate shift_it;
//use shift_it::raw_call;

#[test]
fn request_bytes_for_a_simple_request() {
    let mut rq = shift_it::http_request::HttpRequest::new("http://www.purple.com/").unwrap();
    rq.add_header("Accept", "text/html");

    let rq_str = String::from_utf8(rq.request_bytes()).expect("could not decode string bytes");

    assert_eq!(rq_str, "GET / HTTP/1.1\r\nHost: www.purple.com\r\nAccept: text/html\r\n\r\n");
}

#[test]
fn request_bytes_with_full_query() {
    let rq = shift_it::http_request::HttpRequest::new("https://www.google.co.uk/search?q=term&oq=term&sourceid=chrome&ie=UTF-8#q=term&start=10").unwrap();

    let rq_str = String::from_utf8(rq.request_bytes()).expect("could not decode string bytes");

    assert_eq!(rq_str, "GET /search?q=term&oq=term&sourceid=chrome&ie=UTF-8#q=term&start=10 HTTP/1.1\r\nHost: www.google.co.uk\r\n\r\n");
}

#[test]
fn request_bytes_with_multiple_headers() {
    let mut rq = shift_it::http_request::HttpRequest::new("https://me.com").unwrap();
    rq.add_header("Accept", "text/html");
    rq.add_header("Accept", "application/json");
    rq.add_header("Accept", "*/*");
    rq.add_header("User-Agent", "rust");

    let rq_str = String::from_utf8(rq.request_bytes()).expect("could not decode string bytes");

    assert_eq!(rq_str, "GET / HTTP/1.1\r\nHost: me.com\r\nAccept: text/html,application/json,*/*\r\nUser-Agent: rust\r\n\r\n");
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
