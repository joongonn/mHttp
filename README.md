# mHttp
Low footprint C# library (.NET 4.5) for standing up RESTful services.

Developed using [MonoDevelop 5.10](http://www.monodevelop.com/) on [Mono 4.2.1](http://www.mono-project.com/) (on Xubuntu 14.04).


## How to build
**IDE**: Open and build `mHttp.sln` in MonoDevelop 5 (also tested with Visual Studio Community 2015).

**Command-line**: Otherwise, to build on the command-line in *nx environment with [Mono installed](http://www.mono-project.com/docs/getting-started/install/linux/):
```shell
$ xbuild /p:Configuration=Release mHttp.sln
```
To produce the `mHttp.dll` artifact under the `mHttp/bin/Release` directory.

## Hello World
This example uses the [Mono C# command-line REPL](http://www.mono-project.com/docs/tools+libraries/tools/repl/).

**1.** Start a C# REPL session, referencing the `mhttp.dll` assembly (and its dependencies):
```shell
$ csharp -r:mHttp.dll,NLog.dll
Mono C# Shell, type "help;" for help

Enter statements below.
csharp> using m.Http;
```
**2.** Instantiate the *route table* with a single route:
```shell
csharp> var routeTable = new RouteTable(
      >   Route.Get("/").With((request) => new TextResponse("Hello " + request.Headers["User-Agent"]))
      > );
```
**3.** Instantiate and start the server *backend* listening on port 8080:
```shell
csharp> var server = new HttpListenerBackend("*", 8080);
csharp> server.Start(routeTable);
csharp>  
```
**4.** Hit the endpoint:
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
