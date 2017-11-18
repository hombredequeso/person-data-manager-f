const frisby = require('frisby');

describe('general health check tests', function() {

    it('GET /health returns 200', function(doneFn) {
      frisby.get('http://localhost:8080/health')
        .expect('status', 200)
        .done(doneFn);
    });

})

