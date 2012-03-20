set MASTER=net.tcp://localhost:11080

rem start FloatingQueue.Server\bin\Debug\FloatingQueue.Server.exe -pubport=10080 -intport=11080 -id=0 -m 
start FloatingQueue.Server\bin\Debug\FloatingQueue.Server.exe -pubport=10081 -intport=11081 -id=1 -nodes=%MASTER%
start FloatingQueue.Server\bin\Debug\FloatingQueue.Server.exe -pubport=10082 -intport=11082 -id=2 -nodes=%MASTER%
start FloatingQueue.TestClient\bin\Debug\FloatingQueue.TestClient.exe


rem -pubport=10081 -intport=11081 -id=1 -nodes=net.tcp://localhost:11080
rem -pubport=10082 -intport=11082 -id=2 -nodes=net.tcp://localhost:11080