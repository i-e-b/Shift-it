Shift-it
========

Collection of low-level HTTP and FTP file transfer tools

https://www.nuget.org/packages/Shift-it/

Why?
-----
.Net has some pretty good FTP and HTTP libraries built in. However, they aren't amazingly unit-test friendly. They also wrap a lot of status results in exceptions.
The protocol strictness of the libraries is also problematic when dealing with the general internet.

FTP
-----
An old and battle hardened FTP client library, works with many
old and cranky FTP servers that the standard .Net libraries can't handle.

HTTP
----
A HTTP client that can accept a reasonable level of invalid protocol from servers.
This is a blocking, synchronous library that uses only .Net sockets, not WebClient or HttpWebRequest, and has a lot of replaceable components.

Example GET:
```csharp
var rq = new HttpRequestBuilder().Get(new Uri("https://www.nuget.org/")).Build();
using (var result = new HttpClient().Request(rq)) {
    var body = result.BodyReader.ReadStringToLength();
    // . . .
}
```

Example POST string data, without reading result:
```csharp
var client = new HttpClient();
var postRq = new HttpRequestBuilder().Put(new Uri("https://target.example.com/resource")).Build("Hello, world");
client.Request(postRq).Dispose();
```

Example with upload and download with files and progress:
```csharp
long uploadBytes = 0;
long downloadBytes = 0;

using (var fs = File.OpenRead(@"C:\my\file.dat"))
{
    var rq = new HttpRequestBuilder()
    .Post(new Uri("http://myserver.com/whatever"))
    .Build(fs, fs.Length);

    using (var result = new HttpClient().Request(rq, b => uploadBytes = b))
    {
        var responseString = result.BodyReader.ReadStringToLength(b => downloadBytes = b);
    }
}
```

Performance
===========
Thanks to [skolima](https://github.com/skolima) for these:

```
=== 14MB file

Measured on Windows
Time taken        Mem         Method
->2849ms          29MB        Web client
->2657ms          20MB        Web Client - stream copy, no hash
->2647ms          16MB        Web Client - stream copy, Shift-It hash
->1575ms          0MB         ShiftIt [current]
->1592ms          0MB         ShiftIt - no hashing [current]
->1616ms          0MB         ShiftIt - no compression [current]
->1574ms          0MB         ShiftIt - no hashing, no compression [current]


Measured on Mono + Linux + fast connection
Time taken        Mem     Method
->1763ms          30MB    Web client
->403ms           1MB     Web Client - stream copy, no hash
->651ms           16MB    Web Client - stream copy, Shift-It hash
->755ms           0MB     ShiftIt [current]
->603ms           0MB     ShiftIt - no hashing [current]
->762ms           0MB     ShiftIt - no compression [current]
->669ms           0MB     ShiftIt - no hashing, no compression [current]
->833ms           0MB     ShiftIt [previous]
->602ms           0MB     ShiftIt - no hashing [previous]
->903ms           0MB     ShiftIt - no compression [previous]
->601ms           0MB     ShiftIt - no hashing, no compression [previous]

=== 191MB file

Measured on Windows
Time taken        Mem        Method
->35378ms       638MB   Web client
->34211ms       334MB   Web Client - stream copy, no hash
->35382ms       320MB   Web Client - stream copy, Shift-It hash
->20292ms       0MB     ShiftIt [current]
->20157ms       0MB     ShiftIt - no hashing [current]
->20446ms       0MB     ShiftIt - no compression [current]
->19689ms       0MB     ShiftIt - no hashing, no compression [current]

Measured on Mono + Linux + fast connection
Time taken        Mem        Method
->17435ms       383MB   Web client
->5843ms        69MB    Web Client - stream copy, no hash
->9327ms        260MB   Web Client - stream copy, Shift-It hash
->12948ms       0MB     ShiftIt [current]
->8529ms        0MB     ShiftIt - no hashing [current]
->13472ms       0MB     ShiftIt - no compression [current]
->8108ms        0MB     ShiftIt - no hashing, no compression [current]
->13584ms       0MB     ShiftIt [previous]
->8237ms        0MB     ShiftIt - no hashing [previous]
->10648ms       0MB     ShiftIt - no compression [previous]
->7308ms        0MB     ShiftIt - no hashing, no compression [previous]
```

Todo
-----
* Digest authentication
* chunked upload


