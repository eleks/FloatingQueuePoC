set MASTER=net.tcp://localhost$10080$11080$0$master
set SLAVE1=net.tcp://localhost$10081$11081$1
set SLAVE2=net.tcp://localhost$10082$11082$2
start FloatingQueue.Server\bin\Debug\FloatingQueue.Server.exe -pubport=10080 -intport=11080 -id=0 -s -m -nodes=%SLAVE1%;%SLAVE2%
rem start FloatingQueue.Server\bin\Debug\FloatingQueue.Server.exe -pubport=10081 -intport=11081 -id=1 -s -nodes=%MASTER%;%SLAVE2%
start FloatingQueue.Server\bin\Debug\FloatingQueue.Server.exe -pubport=10082 -intport=11082 -id=2 -s -nodes=%MASTER%;%SLAVE1%
start FloatingQueue.TestClient\bin\Debug\FloatingQueue.TestClient.exe


rem -pubport=10081 -intport=11081 -id=1 -s -nodes=net.tcp://localhost$10080$11080$0$master;net.tcp://localhost$10082$11082$2
rem -pubport=10082 -intport=11082 -id=2 -s -nodes=net.tcp://localhost$10080$11080$0$master;net.tcp://localhost$10081$11081$1