const frisby = require('frisby');
const uuidV4 = require('uuid/v4');

const createdStatusCode = 201;
const okStatusCode = 200;

describe("/api/person", () => {
    describe("POST and GET", () => {
        it('POST with a valid body returns 201(created) and newly created entity', function(done) {
            const personId = uuidV4();
            const postBody = 
                    {
                        "id" : personId,
                            "name": "john smith"
                    };
            frisby.post('http://localhost:8080/api/person', postBody)
                .expect('status', createdStatusCode)
                .expect('json', postBody)
                .done(done);
        });

        it('GET /api/person retrieves person', function(done) {

            const personId = uuidV4();
            const postBody = 
                    {
                        "id" : personId,
                            "name": "john smith"
                    };
            frisby.post('http://localhost:8080/api/person', postBody)
                .then (res => {
                    return frisby.get(`http://localhost:8080/api/person/${personId}`)
                            .expect('status', okStatusCode)
                            .expect('json', postBody)
                })
                .done(done);
        });
    });
});
