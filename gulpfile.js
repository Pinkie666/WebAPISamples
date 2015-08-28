var gulp = require('gulp'),
    chug = require('gulp-chug'),
    http = require('http'),
    path = require('path'),
    fs = require('fs'),
    crypto = require('crypto');

var specPath = "./spec.json";

gulp.task('spec', function(cb) {
    http.get({
        hostname: process.env.API_HOST || 'services.ci.reachmail.net',
        path: '/spec',
        headers: { accept: 'application/json' }
    }, 
    function(response) {
        response.pipe(fs.createWriteStream(specPath));
        cb();
    });
});

gulp.task('ci', ['spec'], function() {
    return gulp.src('./*/gulpfile.js')
        .pipe(chug({ 
            tasks: ['ci'], 
            args: process.argv.slice(3) 
        }));
});

gulp.task('deploy', ['spec'], function() {
//    if (!hasSpecChanged())
//    {
//        console.log('API spec has not changed so not deploying.');
//        return;
//    }
    return gulp.src('./*/gulpfile.js')
        .pipe(chug({ 
            tasks: ['deploy'], 
            args: process.argv.slice(3)
        }));
});

function hasSpecChanged()
{
    var oldHashPath = 'd:/teamcity/shared/';
    if (!fs.existsSync(oldHashPath)) return true;
    oldHashPath = path.join(oldHashPath, 'apischema.hash');
    var oldHash = fs.existsSync(oldHashPath) ? fs.readFileSync(oldHashPath) : null;
    var md5 = crypto.createHash('md5');
    md5.update(fs.readFileSync(specPath));
    var newHash = md5.digest('hex');
    fs.writeFileSync(oldHashPath, newHash);
    return oldHash != newHash;
}
