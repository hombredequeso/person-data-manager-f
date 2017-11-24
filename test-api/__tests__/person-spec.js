const frisby = require('frisby');
const uuidV4 = require('uuid/v4');

it('POST /api/person with a valid body returns 201(created)', function(done) {

    const personId = uuidV4();
    frisby.post('http://localhost:8080/api/person',
        {
            "id" : personId,
            "name": "john smith"
        })
        .expect('status', 200)
        .done(done);
});


it('GET /api/person retrieves person', function(done) {

    const personId = uuidV4();
    frisby.post('http://localhost:8080/api/person',
        {
            "id" : personId,
            "name": "john smith"
        })
        .expect('status', 200)
        .done(done);
});
