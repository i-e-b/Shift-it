Shift-it
========

Collection of low-level HTTP and FTP file transfer tools

Why?
-----
.Net has some pretty good FTP and HTTP libraries built in. However, they aren't amazingly unit-test friendly. They also wrap a lot of status results in exceptions.
The protocol strictness of the libraries is also problematic when dealing with the general internet.

FTP
-----
An old and battle hardend FTP client library, works with many
old and cranky FTP servers that the standard .Net libraries can't handle.

HTTP
----
A HTTP client that can accept a reasonable level of invalid protocol from servers.
This is a blocking, synchronous library that uses only .Net sockets, not WebClient or HttpWebRequest, and has a lot of replaceable components.

Todo:
* Digest authentication
* chunked upload
