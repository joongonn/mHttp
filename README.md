# mHttp
Low footprint C# HTTP/1.1 server library (.NET 4.5) for standing up RESTful services.

Developed using [MonoDevelop 5.10](http://www.monodevelop.com/) on [Mono 4.2.1](http://www.mono-project.com/) (on Xubuntu 14.04).

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

Proceed to the [wiki](https://github.com/joongonn/mHttp/wiki) to learn more.
