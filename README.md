# Writing Highly Testable Code

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
4. Constructors are for declaring dependency needs, **not initialization**!

### Testing Suggestions

5. Minimize magic dependencies.
	- Magic Dependencies are often created during entry point setup and there's generally no way to ascertain this setup until an error occurs.
6. Every language has gotchas. 
    - .NET languages can be challenging to mock and fake.

## My Thoughts

Writing code that is highly testable via unit tests requires exactly two proactive changes. These are commandments one and two and form the L and A from La.S.I.C. 

First, write code that is loosely coupled to the entry point. Virtually every application has the following dichotomy - the value-add and the entry point. The value-add is the reason you (or others!) desire to use your application. The entry point is roughly everything else. The entry point is the console application, the operating system service, the web api ... you get the picture. Maybe you're asking, "how do I loosely couple the value-add from the entry point?".

Second, simultaneously write the code and the unit tests. This is the most powerful and straightforward coding strategy to identify value-add code that is tightly coupled to the entry point *because the unit tests are another entry point from where your code needs to be callable*. Write a bit of code then a little bit of testing. Repeat. Do not totally defer one activity in favor of the other.

The primary responsibility of an entry point is to provide us with a place where we can initialize the dependencies required to run our value-add code. Entry points are required because we absolutely need some mechanism to run stuff. 

Separating value-add code from the entry point allows us to more easily reason about the actual needs of our application. Allow your value-add code to declare the needs that need fulfilled. It will follow that the implementations vary based on the type of entry point used. 

I created the Domain Chekr application to be a small and digestible application to illustrate two different ways to build the same thing. Initially, I just wrote code to make the thing work. There's absolutely nothing wrong with approach to get the idea off the ground. Next, I applied the ideas of Writing Highly Testable Code to refactor the application to smaller components that are completely decoupled from the entry point. The result is an application - Domain Chekr - that can be hosted in many different scenarios like a CLI (command line interface), web api or gRPC service.

Let's use Domain Chekr to explore an example of declaring needs and fulfilling with implementations. The Domain Chekr application requires an API Key as a means to authenticate a request. We could say that Domain Chekr establishes a contract (e.g. interface) to declare the need. Should Domain Chekr necessarily care about how that need is fulfilled? No, it should not care. 

How the API Key is retrieved becomes an implementation detail that varies by the type of entry point used to invoke the application. When Domain Chekr is hosted in a web api we can create an implementation to retrieve the API Key from a HTTP header. When Domain Chekr is hosted in a console, we can create an implementation to get the value from the console arguments.

Commandments three and four are to share common software development techniques to fulfill commandment two. Commandment three means to use roughly the five design principles of SOLID. Use SOLID principles as guardrails and guidelines, not dogma. Commandment four is a largely pet peeve that is difficult to violate because of commandment three.

Commandments five and beyond are good to keep in mind. Following commandments one through four means that all other commandments will - by virtue of best practices - never cause a problem.

### What are dependencies?
- Databases, file systems, FTP servers, network connections, etc.

### OOP - Constructors
- The constructor of an entry point is to initialize dependencies.
- The constructor of a model/POCO is to allow the creation of valid instances.
	- Or use the factory pattern to create instances.
- The constructor of everything else is to accept already initialized dependencies.

## Domain Chekr

*Demonstrating the principals of LaSIC through a pseudo-application.*

Domain Chekr is serivce to provide safety reports of domain names. We maintain a crawler constantly traversing the internet attempting to determine if a domain is safe to visit or is it has been compromised through some form of spam, malware or ransomware.

Domain Chekr provides a web api for subscribers to get the latest report card for any given domain. The `/chekr` routes accepts a query parameter `domain`.

Subscribers are provided with an api key that is required for all calls in to the Domain Chekr system. The api key is included as a header with the name `x-chekr-api-key`.

Subscribers have rate limits that are tied to the api key. The rate limiter works on a rolling 60 second window. An error is returned if the calls exceed the established rate limit.

Successfully validated calls then check the `domain` query param to get the input. Subscribers can send the data as the bare domain (ex. microsoft.com) or fully loaded (https://www.microsoft.com/page/something.aspx). The call is considered validated when it has a valid api key and is not rate limited.

### Build Dependencies

1. .NET 5
1. Visual Studio 2019 or VS Code