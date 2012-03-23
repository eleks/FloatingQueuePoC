set MASTER=net.tcp://localhost:11081
start FloatingQueue.Server\bin\Debug\FloatingQueue.Server.exe -pubport=10082 -intport=11082 -id=2 -nodes=%MASTER%
