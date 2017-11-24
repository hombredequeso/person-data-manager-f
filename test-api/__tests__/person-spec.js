const frisby = require('frisby');

it('POST adds a new person', function(done) {

  frisby.post('http://localhost:8080/api/person',
      {
          "name": "john smith"
      })
    .expect('status', 200)
    .done(done);
});


it('GET /api/person retrieves person', function(done) {

  frisby.post('http://localhost:8080/api/person',
      {
          "name": "john smith"
      })
    .expect('status', 200)
    .done(done);
});
