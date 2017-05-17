# Historia

## Summary
Historia is a packet logging tool based off of [Cyrenia](https://github.com/r1emu/Cyrenia).

## How To Use
### Commandline Arguments
```
  -b, --barrack    Required. address for barrack proxy <address:port>.

  -z, --zone       Required. address for zone proxy <address:port>.

  -w, --web        Required. web server address for serving configuration
                   <address:port>.

  -p, --path       Required. path to client installation.

  --help           Display this help screen.
```

### Example
```
Historia.exe --barrack="127.0.0.1:7001" --zone="127.0.0.1:7024" --web="127.0.0.1:8080" --path="D:\games\client"
```

## Notes
* Historia requires administrator privileges when executing. This is because of the use of [HttpListener](https://msdn.microsoft.com/en-us/library/system.net.httplistener(v=vs.110).aspx) for the web proxy.
* Client configuration files are backed up to `client.xml.bak`, but you should make certain to maintain your own backup as well.
* Packets can be viewed in the folder named `packets` next to the executable location.

## Credits
Aura Project team for crypto.  
R1EMU team for Cyrenia.
