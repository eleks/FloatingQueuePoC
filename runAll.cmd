start FloatingQueueServer\bin\Debug\FloatingQueue.Server.exe -port=8080 -m -nodes=net.tcp://localhost:8081
start FloatingQueueServer\bin\Debug\FloatingQueue.Server.exe -port=8081 -nodes=net.tcp://localhost:8080$master
rem for some reasons windows doesn't allow to run more than 2 http servers at the same time. TODO: think about it
rem start FloatingQueueServer\bin\Debug\FloatingQueueServer.exe -port=9090 
start TestClient\bin\Debug\FloatingQueue.TestClient.exe