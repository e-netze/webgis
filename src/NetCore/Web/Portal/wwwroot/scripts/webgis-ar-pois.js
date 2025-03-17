$(document).ready(function () {
    webgis.init(function () {
        console.log('webgis-initialized');

        webgis.ajax({
            url: webgis.baseUrl + '/rest/services/' + serviceId + '/queries/' + queryId,
            type: 'post',
            data: webgis.hmac.appendHMACData({ '#oids#': fids, f: 'json', c: 'query' }),
            success: function (result) {
                console.log(result);

                let scene = document.querySelector('a-scene');

                const f1 = function (ev) {
                    console.log('scene-click');
                    console.log(ev);
                };
                scene.addEventListener('touchstart', f1, false);
                scene.addEventListener('click', f1, false);
                scene.addEventListener('pointerdown', f1, false);

                var x = 0;
                //result.features.forEach((feature) => {
                //    let latitude = feature.geometry.coordinates[1];
                //    let longitude = feature.geometry.coordinates[0];

                //    let model = document.createElement('a-text');
                //    model.setAttribute('value', 'Poi ' + x++);
                //    model.setAttribute('gps-entity-place', `latitude: ${latitude}; longitude: ${longitude};`);
                //    model.setAttribute('scale', '120 120 120');
                //    model.setAttribute('look-at', '[gps-camera]')

                //    console.log(`latitude: ${latitude}; longitude: ${longitude};`);

                //    //setModel(models[modelIndex], model);

                //    //model.setAttribute('animation-mixer', '');

                //    //document.querySelector('button[data-action="change"]').addEventListener('click', function () {
                //    //    var entity = document.querySelector('[gps-entity-place]');
                //    //    modelIndex++;
                //    //    var newIndex = modelIndex % models.length;
                //    //    setModel(models[newIndex], entity);
                //    //});

                //    //<a-text
                //    //    value="This content will always face you."
                //    //    look-at="[gps-camera]"
                //    //    scale="120 120 120"
                //    //    gps-entity-place="latitude: <add-your-latitude>; longitude: <add-your-longitude>;"
                //    //></a-text>

                //    console.log(model.getAttribute('gps-entity-place'));

                //    scene.appendChild(model);
                //});

                result.features.forEach((feature) => {
                    let latitude = feature.geometry.coordinates[1];
                    let longitude = feature.geometry.coordinates[0];

                    // add place icon
                    const icon = document.createElement('a-image');
                    icon.setAttribute('gps-entity-place', `latitude: ${latitude}; longitude: ${longitude};`);
                    icon.setAttribute('name', feature.properties._fulltext);
                    icon.setAttribute('src', webgis.css.imgResource('marker_blue_' + ((++x)) + '.png', 'markers'));

                    icon.setAttribute('onclick', 'alert(1)');

                    //alert(feature.properties._fulltext);

                    // for debug purposes, just show in a bigger scale, otherwise I have to personally go on places...
                    icon.setAttribute('scale', '20, 20');

                    icon.addEventListener('loaded', () => window.dispatchEvent(new CustomEvent('gps-entity-place-loaded')));

                    const clickListener = function (ev) {
                        console.log('click');
                        ev.stopPropagation();
                        ev.preventDefault();

                        const name = ev.target.getAttribute('name');

                        alert(name);

                        const el = ev.detail.intersection && ev.detail.intersection.object.el;

                        if (el && el === ev.target) {
                            const label = document.createElement('span');
                            const container = document.createElement('div');
                            container.setAttribute('id', 'place-label');
                            label.innerText = name;
                            container.appendChild(label);
                            document.body.appendChild(container);

                            setTimeout(() => {
                                container.parentElement.removeChild(container);
                            }, 1500);
                        }
                    };

                    icon.addEventListener('pointerdown', clickListener, false);
                    icon.addEventListener('touchstart', clickListener, false);
                    icon.addEventListener('click', clickListener, false);

                    scene.appendChild(icon);                   
                });
            }
        });
    });
});