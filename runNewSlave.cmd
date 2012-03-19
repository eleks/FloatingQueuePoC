set MASTER=net.tcp://localhost:11081
start FloatingQueue.Server\bin\Debug\FloatingQueue.Server.exe -pubport=10083 -intport=11083 -id=3 -nodes=%MASTER%
