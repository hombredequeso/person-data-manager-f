const frisby = require('frisby');

test('GET /health returns 200', function(done) {
  frisby.get('http://localhost:8080/health')
    .expect('status', 200)
    .done(done);
});

test('GET /health/db returns 200', function(done) {
  frisby.get('http://localhost:8080/health/db')
    .expect('status', 200)
    .done(done);
});

