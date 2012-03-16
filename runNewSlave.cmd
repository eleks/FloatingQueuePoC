set MASTER=net.tcp://localhost:11081$1$master
set SLAVE2=net.tcp://localhost:11082$2
start FloatingQueue.Server\bin\Debug\FloatingQueue.Server.exe -pubport=10083 -intport=11083 -s =id=3 -nodes=%MASTER%;%SLAVE2%
