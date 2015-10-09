// Import some modules
var
    Q = require('q'),
    fs = require('fs'),
    del = require('del'),
    gulp = require('gulp'),
    gitTagV = require('gulp-tag-version'),
    run = require('gulp-run'),
    bump = require('gulp-bump'),
    filter = require('gulp-filter'),
    map = require('map-stream')
;

// Define some basic config
var config =
{
    srcProject: "./src/Graceful/project.json",
    testProject: "./tests/Graceful.Tests/project.json",
    binDir: "./bin"
};

/**
 * Deletes everything inside the bin folder
 * to ensure we don't get stale artifacts.
 */
gulp.task('clean', function (done)
{
    del([config.binDir + '/**/*'], done);
});

/**
 * Increments the version number of the main project.json file.
 */
gulp.task('bump', function()
{
    gulp.src(config.srcProject)
    .pipe(bump())
    .pipe(gulp.dest(config.srcProject.replace('project.json', '')));
});

/**
 * Restores all NuGet Packages for both src and tests projects.
 */
gulp.task('restore', function(done)
{
    run('dnu restore ').exec(done);
});

/**
 * Runs Unit Tests, using Xunit.
 */
gulp.task('test', function(done)
{
    run('dnx '+config.testProject+' test').exec(done);
});

/**
 * Assuming the Unit Tests pass this will create a new NuGet Package.
 */
gulp.task('package', ['clean', 'bump', 'restore', 'test'], function(done)
{
    run
    (
        'dnu pack '+config.srcProject+
        ' --configuration Release '+
        '--out '+config.binDir
    ).exec(done);
});

/**
 * Creates a new git tag and pushes to github.
 */
gulp.task('tag', ['package'], function()
{
    var deferred = Q.defer();

    var complete = 0, done = function()
    {
        ++complete; if (complete == 2) deferred.resolve();
    };

    run('git add -A').exec(function()
    {
        run("git commit -m NewBuild").exec(function()
        {
            run('git push').exec(done);

            gulp.src(config.srcProject).pipe(gitTagV()).on('end', function()
            {
                run('git push --tags').exec(done);
            });
        });
    });

    return deferred.promise;
});

/**
 * Publishes the Nuget Package to nuget.org
 */
gulp.task('publish', ['tag'], function()
{
    return gulp.src
    ([
        config.binDir + '/Release/*.nupkg',
        '!'+config.binDir+'/Release/*.symbols.nupkg'
    ])
    .pipe(map(function(file, cb)
    {
        run('nuget push ' + file.path).exec(function()
        {
            cb(null, file);
        });
    }));
});
