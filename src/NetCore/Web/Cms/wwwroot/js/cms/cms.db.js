CMS.db = function () {
    "use strict";
    //check for support
    if (!('indexedDB' in window)) {
        console.log('This browser doesn\'t support IndexedDB');
        return;
    }
    var dbName = 'cms-' + CMS.id;
    var id = function (id) { /*return CMS.id + '-' +*/ id; };
    function openDb(onReady) {
        var db;
        var openRequest = indexedDB.open(dbName, 5);
        openRequest.onupgradeneeded = function (event) {
            console.log('upgradedb');
            var upgradeDb = openRequest.result;
            if (!upgradeDb.objectStoreNames.contains('pins')) {
                var favsOS = upgradeDb.createObjectStore('pins', {
                    keyPath: 'path',
                    autoIncrement: true
                });
            }
        };
        openRequest.onerror = function (event) {
            console.log("web app not allowed to use IndexedDB?!");
        };
        openRequest.onsuccess = function (event) {
            db = event.target.result;
            db.onerror = function (event) {
                console.log("Database error: " + event.srcElement.error);
            };
            onReady(db);
            db.close();
        };
    }
    ;
    /////////////////////////////////////////
    //           Pins
    /////////////////////////////////////////
    this.addPin = function (name, path, onsuccess) {
        openDb(function (db) {
            var tx = db.transaction('pins', 'readwrite');
            var store = tx.objectStore('pins');
            var item = {
                path: path,
                name: name,
                created: new Date().getTime()
            };
            store.add(item).onsuccess = function (event) {
                if (onsuccess)
                    onsuccess(event.target.result);
            };
            return tx.complete;
        });
    };
    this.removePin = function (path, onsuccess) {
        openDb(function (db) {
            var tx = db.transaction('pins', 'readwrite');
            var store = tx.objectStore('pins');
            store.delete(path).onsuccess = function (event) {
                if (onsuccess)
                    onsuccess(event.target.result);
            };
            return tx.complete;
        });
    };
    this.getAllPins = function (onsuccess, onerror) {
        openDb(function (db) {
            var tx = db.transaction('pins', 'readwrite');
            var store = tx.objectStore('pins');
            var request = store.getAll();
            request.onsuccess = function (event) {
                var items = [];
                for (var i in event.target.result) {
                    var item = event.target.result[i];
                    items.push(item);
                }
                onsuccess(items);
            };
            request.onerror = function (event) {
                if (onerror)
                    onerror(event);
            };
            return tx.complete;
        });
    };
};
