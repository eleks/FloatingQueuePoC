set MASTER=net.tcp://localhost$10081$11081$1$master
set SLAVE2=net.tcp://localhost$10082$11082$2
start FloatingQueue.Server\bin\Debug\FloatingQueue.Server.exe -pubport=10083 -intport=11083 -id=3 -nodes=%MASTER%;%SLAVE2%
