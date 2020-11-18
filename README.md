# Writing Testable Code

## La.S.I.C
*Helping to see clearly in to writing testable code.*

**L**oosely couple code from entry point. \
**S**imultaneously write code and write test. \
**I**solate dependencies using classes and interfaces. \
**C**onstructors are for dependency initialization.

### Testing Commandments
1. Loosely couple your code from the entry point.
	- Entry points are consoles, apis, services, GUI's, etc.
		- The entry point always performs some type of setup or initialization.
2. Write your code and the tests at approximately the same time.
	- Will quickly discover what designer patterns are easy to easy, and which suck to test.
3. Isolate dependencies using classes and interfaces.
	- Use classes and interfaces to abstract/isolate/wrap away behavior with dependencies.
4. Constructors are for dependency initialization, **not behavior initialization**!

### Testing Suggestions

5. Minimize magic dependencies.
	- Magic Dependencies are often created during entry point setup and there's generally no way to ascertain this setup until an error occurs.
6. Every language has gotchas. 
    - .NET languages can be challenging to mock and fake.


### What are dependencies?
- Databases, file systems, FTP servers, network connections, etc.


The constructor of an entry point is to accept already initialized dependencies.
The constructor of a model/POCO is to allow the creation of valid instances.
	Or use the factory pattern to create instances.


## Domain Chekr

*Demonstrating the principals of LaSIC through a pseudo-application.*

Domain Chekr is serivce to provide safety reports of domain names. We maintain a crawler constantly traversing the internet attempting to determine if a domain is safe to visit or is it has been compromised through some form of spam, malware or ransomware.

Domain Chekr provides a web api for subscribers to get the latest report card for any given domain. The `/chekr` routes accepts a query parameter `domain`.

Subscribers are provided with an api key that is required for all calls in to the Domain Chekr system. The api key is included as a header with the name `x-chekr-api-key`.

Subscribers have rate limits that are tied to the api key. The rate limiter works on a rolling 60 second window. An error is returned if the calls exceed the established rate limit.

Successfully validated calls then check the `domain` query param to get the input. Subscribers can send the data as the bare domain (ex. microsoft.com) or fully loaded (https://www.microsoft.com/page/something.aspx). The call is considered validated when it has a valid api key and is not rate limited.
