# Instructions for candidates

This is the .NET version of the Payment Gateway challenge. If you haven't already read this [README.md](https://github.com/cko-recruitment/) on the details of this exercise, please do so now. 

## Template structure
```
src/
    PaymentGateway.Api - a skeleton ASP.NET Core Web API
test/
    PaymentGateway.Api.Tests - an empty xUnit test project
imposters/ - contains the bank simulator configuration. Don't change this

.editorconfig - don't change this. It ensures a consistent set of rules for submissions when reformatting code
docker-compose.yml - configures the bank simulator
PaymentGateway.sln
```

Feel free to change the structure of the solution, use a different test library etc.

## Solution - Joseph Hoang
General steps 
1. Firstly started with running the bank simulator locally to check its working
2. Then started to look into calling the api via a client and implemented the IBankClient/BankClient with a singular method which calls the simulator bank and returns a response. With simple unit tests to validate the request to the Bank Simulator
3. Created some integration tests for the BankClient calling the simulator
4. Started to create simple unit tests for validating the PaymentsController Post endpoint and then refactored after all tests passed 
5. Dependency injected the BankClient for the controller to have access 
6. Implemented the call to the BankClient with consideration of mapping the PostPaymentResponse to a class which was more suited the Bank Simulator as there were subtle differences

Considerations made
1. Assumption was that when there were validation issues with the request that a 400 Bad Request response is returned with the status and reason for failure instead of returning the whole response back
2. The casing for the request and response back to the client would be snake case lower format