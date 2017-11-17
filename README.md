# person-data-manager-f
API for managing person data, in F#, using Suave and Elasticsearch


# Person Data Manager F

person-data-manager-f is about managing large volumes of person data with respect to categorization and searching.

It is a personal experimental project with a focus on using the following technologies:
* [Elasticsearch](https://www.elastic.co/)
* F#
* [Suave] (https://suave.io/) based api
* [paket] (https://fsprojects.github.io/Paket/) for package dependency management

## Getting Started

### Prerequisites

* Something to compile an F# dotnet framework application with (e.g. Visual Studio 2017)
* [Docker] (https://www.docker.com/): using docker-compose for containerized development infrastructure
* [Postman] (https://www.getpostman.com/): optional, but probably the easiest way to play around with the api

### Running in a Development Environment

To run in a development environment:

* Run docker compose in the project root directory to get an Elasticsearch database up and running

```
> docker-compose up
```

* Compile and run the person-data-manager-f solution.

If all is going well, it should be possible to test the api with Postman with the collection of queries found in the data/ directory.

/health : gets the api status
/health/db : gets the elasticsearch database status


## Running the tests

TODO

## Built With

* [Elasticsearch](https://www.elastic.co/) - search engine/database
* [Suave] (https://suave.io/) - web server, in F#
* [Paket] (https://fsprojects.github.io/Paket/) - package dependency management
* [Docker] (https://www.docker.com/) - for containerized infrastructure

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

