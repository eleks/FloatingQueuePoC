set MASTER=net.tcp://localhost:11080$master
set SLAVE1=net.tcp://localhost:11081$1
set SLAVE2=net.tcp://localhost:11082$2
rem start FloatingQueue.Server\bin\Debug\FloatingQueue.Server.exe -pubport=10080 -intport=11080 -m -nodes=%SLAVE1%;%SLAVE2%
start FloatingQueue.Server\bin\Debug\FloatingQueue.Server.exe -pubport=10081 -intport=11081 -id=1 -nodes=%MASTER%;%SLAVE2%
start FloatingQueue.Server\bin\Debug\FloatingQueue.Server.exe -pubport=10082 -intport=11082 -id=2 -nodes=%MASTER%;%SLAVE1%
start FloatingQueue.TestClient\bin\Debug\FloatingQueue.TestClient.exe


rem -pubport=10080 -intport=11080 -m -nodes=net.tcp://localhost:11081$1;net.tcp://localhost:11082$2


