set MASTER=net.tcp://localhost:11080$master
set SLAVE1=net.tcp://localhost:11081$1
set SLAVE2=net.tcp://localhost:11082$2
start FloatingQueueServer\bin\Debug\FloatingQueue.Server.exe -port=11080 -m -nodes=%SLAVE1%;%SLAVE2%
start FloatingQueueServer\bin\Debug\FloatingQueue.Server.exe -port=11081 -id=1 -nodes=%MASTER%;%SLAVE2%
start FloatingQueueServer\bin\Debug\FloatingQueue.Server.exe -port=11082 -id=2 -nodes=%MASTER%;%SLAVE1%
start TestClient\bin\Debug\FloatingQueue.TestClient.exe