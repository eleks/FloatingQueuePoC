start FloatingQueueServer\bin\Debug\FloatingQueueServer.exe -port=8080
start FloatingQueueServer\bin\Debug\FloatingQueueServer.exe -port=8081
rem for some reasons windows doesn't allow to run more than 2 http servers at the same time. TODO: think about it
rem start FloatingQueueServer\bin\Debug\FloatingQueueServer.exe -port=9090 
start TestClient\bin\Debug\TestClient.exe