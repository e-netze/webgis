if ('serviceWorker' in navigator) {
    window.addEventListener('load', function () {

        var isRegistered = false;
        this.navigator.serviceWorker.getRegistrations().then(registrations => {
            //console.log('serviceworker registrations');
            //console.log(registrations);
            isRegistered = registrations.length > 0;
        });

        if (window.unregister_serviceworker === true && isRegistered == false)
            return;

        // Register
        var scriptUrl = service_worker_scope + '/serviceworker';
        navigator.serviceWorker.register(scriptUrl, /*{ scope: service_worker_scope }*/).then(function (registration) {
            // Registration was successful
            console.log('ServiceWorker registration successful with scope: ', registration.scope);

            if (window.unregister_serviceworker === true) {
                // Unregister
                registration.unregister().then(function (boolean) {
                    if (boolean === true)
                        console.log("ServiceWorker registered");
                });
            }

        }, function (err) {
            // registration failed :(
            console.log('ServiceWorker registration failed: ', err);
        });
    });
}