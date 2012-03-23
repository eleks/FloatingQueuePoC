start FloatingQueue.TestClient\bin\Debug\FloatingQueue.TestClient.exe

rem flood 4 250 10 - generate 1000 events in 4 threads in 11 aggregates(10+1)
rem get 1 0 - get event at version 0 of aggregate '1'
rem get 4 10  - get event at version 10 of aggregate '4'
rem get all 7 - get all events of aggregate '7'
rem get all 9 20 - get all events of aggregate '9', starting from version 21