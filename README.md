# mHttp
Low footprint fully managed C# HTTP/1.1 server library (.NET 4.5) for standing up RESTful services.

Developed using [MonoDevelop 5.10](http://www.monodevelop.com/) on [Mono 4.4.1](http://www.mono-project.com/) (on Xubuntu 14.04).

Released under the [MIT License](https://github.com/joongonn/mHttp/blob/master/LICENSE.txt).
## Demo
Check out the sample project at [http://mhttp.net](http://mhttp.net).

## How to build 
| CI | Platform | Status |
| ---- | ---- | ---- |
| Travis CI | Linux/Mono | [![Build Status](https://travis-ci.org/joongonn/mHttp.svg?branch=master)](https://travis-ci.org/joongonn/mHttp) |
| AppVeyor | Windows | [![Build Status](https://ci.appveyor.com/api/projects/status/nu1rvyk7831m3jcm?svg=true)](https://ci.appveyor.com/project/joongonn/mhttp) |

**IDE**: Open and build `mHttp.sln` in MonoDevelop 5 (also tested with Visual Studio Community 2015).

**Command-line**: Otherwise, to build on the command-line in *nx environment with [Mono installed](http://www.mono-project.com/docs/getting-started/install/linux/):
```shell
$ xbuild /p:Configuration=Release mHttp.sln
```
To produce the `mHttp.dll` artifact under the `mHttp/bin/Release` directory.

## Hello World
This example uses the [Mono C# command-line REPL](http://www.mono-project.com/docs/tools+libraries/tools/repl/).

**1.** Start a C# REPL session, loading the `mHttp.dll` assembly:
```shell
$ csharp -r:mHttp.dll
Mono C# Shell, type "help;" for help

Enter statements below.
```

**2.** Import the `m.Http` namespace:
```shell
csharp> using m.Http;
```

**3.** Instantiate the *route table* with a single route:
```shell
csharp> var routeTable = new RouteTable(
      >   Route.Get("/").With(request => new TextResponse("Hello " + request.Headers["User-Agent"]))
      > );
```
**4.** Instantiate and start the server *backend* listening on port 8080:
```shell
csharp> var server = new HttpBackend(System.Net.IPAddress.Any, 8080);
csharp> server.Start(routeTable);
```
**5.** Hit the endpoint:
```shell
$ curl -v http://localhost:8080
> GET / HTTP/1.1
> User-Agent: curl/7.35.0
......
> 
< HTTP/1.1 200 OK
< Content-Type: text/plain
......
< 
Hello curl/7.35.0
```
See the [sample project](https://github.com/joongonn/mHttp/blob/master/mHttp.Sample/Program.cs) for a more involved example.


## Performance
A quick `ab` sampling of the [sample project](https://github.com/joongonn/mHttp/blob/master/mHttp.Sample/Program.cs) at its trivial `/plaintext` endpoint as of **22/7/2016** with the following setup:

* **Server**: i7-3770K; 32gb ram; Xubuntu 14.04.4 LTS
* **Client**: i5-3210M; 16gb ram; Xubuntu 14.04.4 LTS
* **Network**: Direct GB-nic connection
* **Tuning**: `ulimit -n` at `65535` (everything else distro default)
* **Runtime**: Mono 4.4.1

### Server Process
```
10.0.0.1:~$ MONO_GC_PARAMS="nursery-size=128m,max-heap-size=1024m,major=marksweep" mono --server --gc=sgen mHttp.Sample.exe
```
### Client `ab` run
```
10.0.0.2:~$ ab -n 1024000 -c 1024 http://10.0.0.1:8080/plaintext
```
**Results**
```
Concurrency Level:      1024
Time taken for tests:   59.683 seconds
Complete requests:      1024000
Failed requests:        0
Total transferred:      166912000 bytes
HTML transferred:       12288000 bytes
Requests per second:    17157.18 [#/sec] (mean)
Time per request:       59.683 [ms] (mean)
Time per request:       0.058 [ms] (mean, across all concurrent requests)
Transfer rate:          2731.07 [Kbytes/sec] received

Connection Times (ms)
              min  mean[+/-sd] median   max
Connect:        0   29 180.2      0    3009
Processing:     1   30  29.0     25    1635
Waiting:        1   29  29.0     25    1635
Total:          3   59 183.0     27    3054

Percentage of the requests served within a certain time (ms)
  50%     27
  66%     38
  75%     43
  80%     46
  90%     56
  95%     62
  98%   1013
  99%   1030
 100%   3054 (longest request)
```

### Client `ab` run with Keep-Alive
```
10.0.0.2:~$ ab -k -n 1024000 -c 1024 http://10.0.0.1:8080/plaintext
```
**Results**
```
Concurrency Level:      1024
Time taken for tests:   18.470 seconds
Complete requests:      1024000
Failed requests:        0
Keep-Alive requests:    1014115
Total transferred:      202315453 bytes
HTML transferred:       12288000 bytes
Requests per second:    55441.22 [#/sec] (mean)
Time per request:       18.470 [ms] (mean)
Time per request:       0.018 [ms] (mean, across all concurrent requests)
Transfer rate:          10697.00 [Kbytes/sec] received
```
