import * as THREE from '../../lib/three/build/three.module.js';

import { OrbitControls } from '../../lib/three/examples/jsm/controls/OrbitControls.js';

webgis.threeD = function (map, targetId, terrainData) {
    var container, $info, $infoResult, $infoSingleResults, $infoCurrent, $target = $('#' + targetId);

    var camera, controls, scene, renderer;

    var mesh, texture;

    var disposeables = [], clickSpheres = [], clickArrows = [];
    

    terrainData = terrainData;

    var arrayWidth = terrainData.three_d_arraysize[1], arrayDepth = terrainData.three_d_arraysize[0],
        arrayHalfWidth = arrayWidth / 2, arrayHalfDepth = arrayDepth / 2;

    var helper;

    //var ll = map.crs.frameworkElement.projection.project(L.latLng(terrainData.three_d_bbox[0], terrainData.three_d_bbox[1]));
    //var ur = map.crs.frameworkElement.projection.project(L.latLng(terrainData.three_d_bbox[2], terrainData.three_d_bbox[3]));

    var ll = { x: terrainData.three_d_bbox[0], y: terrainData.three_d_bbox[1] };
    var ur = { x: terrainData.three_d_bbox[2], y: terrainData.three_d_bbox[3] };

    var worldX = Math.abs(ur.x - ll.x), worldY = Math.abs(ur.y - ll.y);
    var falseNorthing = (ur.y + ll.y) * .5, falseEasting = (ur.x + ll.x) * .5;

    var raycaster = new THREE.Raycaster();
    var mouse = new THREE.Vector2();

    if (terrainData.three_d_texture == "monochrome") {
        build();
    }
    else {
        $("<div class='webgis-progress-message webgis-progress-message-running'><img class='webgis-progress-message-img' src='" + webgis.baseUrl + "/content/api/img/hourglass/loader1.gif' style='width:16px;height:16px' />&nbsp;Downloading texture image...</div>")
            .appendTo($target);

        var downloadOptions = {};

        downloadOptions.bbox = terrainData.three_d_bbox.toString();
        downloadOptions.bbox_epsg = terrainData.three_d_bbox_epsg;
        downloadOptions.size = [terrainData.three_d_arraysize[1] * 2, terrainData.three_d_arraysize[0] * 2].toString();
        downloadOptions.dpi = 96;
        downloadOptions.format = 'jpg';
        downloadOptions.worldfile = false;
        downloadOptions.displaysize = [terrainData.three_d_arraysize[1] * 4, terrainData.three_d_arraysize[0] * 4].toString();

        var customServices = [];
        if ($.inArray(terrainData.three_d_texture, ["ortho", "orthostreets"])>=0 && terrainData.three_d_texture_ortho_service) {
            var s = terrainData.three_d_texture_ortho_service.split(':');
            customServices.push({
                id: s[0],
                opacity: 1,
                layers: [{ id: s[1], visible: true } ]
            });
        }
        if ($.inArray(terrainData.three_d_texture, ["orthostreets"])>=0 && terrainData.three_d_texture_ortho_service) {
            var s = terrainData.three_d_texture_streets_overlay_service.split(':');
            customServices.push({
                id: s[0],
                opacity: 1,
                layers: [{ id: s[1], visible: true }]
            });
        }

        if (customServices.length > 0) {
            downloadOptions.useCustomServices = customServices;
        }

        console.log('texture', terrainData.three_d_texture);
        console.log('customServices', customServices);

        map.downloadImage(downloadOptions, function (result) {
            if (result.downloadid) {
                terrainData.imageUrl = webgis.baseUrl + '/rest/download/' + result.downloadid + '?contentType=image/jpeg';
            }
            build();
        });
    }

    function build() {
        container = document.getElementById(targetId);
        container.innerHTML = "";

        var $progress = $("<div class='webgis-progress-message webgis-progress-message-running' style='z-index:99999;background:transparent;position:absolute'><img class='webgis-progress-message-img' src='" + webgis.baseUrl + "/content/api/img/hourglass/loader1.gif' style='width:16px;height:16px' />&nbsp;Building 3D Model...</div>")
            .appendTo($(container));

        webgis.delayed(function () {
            console.log(this);
            init();
            animate();
            webgis.delayed(function () {
                $progress.remove();
            }, 1000);
        }, 10);
    };

    function init() {
        //container = document.getElementById(targetId);
        //container.innerHTML = "";

        //console.log($(container).innerWidth(), $(container).innerHeight());
        $(container).css({ padding: '0px', margin: '0px' });

        renderer = new THREE.WebGLRenderer({ antialias: true });
        renderer.setPixelRatio(window.devicePixelRatio);
        renderer.setSize($(container).innerWidth(), $(container).innerHeight() - 2);
        container.appendChild(renderer.domElement);

        scene = new THREE.Scene();
        scene.background = new THREE.Color(0xbfd1e5);

        camera = new THREE.PerspectiveCamera(60, window.innerWidth / window.innerHeight, 10, 20000);

        controls = new OrbitControls(camera, renderer.domElement);
        controls.minDistance = 10;
        controls.maxDistance = 100000;
        controls.maxPolarAngle = Math.PI / 2;
        controls.enablePan = true;
        //

        //var data = generateHeight( arrayWidth, arrayDepth );
        var data = terrainData.three_d_values;


        controls.target.x = 0;
        controls.target.z = 0;
        controls.target.y = data[parseInt(data.length / 2)] + 2;

        camera.position.y = controls.target.y + 200;
        camera.position.x = 0;
        camera.position.z = worldY / 2;
        controls.update();

        //console.log(worldX, worldY);

        var geometry = new THREE.PlaneBufferGeometry(worldX, worldY, arrayWidth - 1, arrayDepth - 1);
        geometry.rotateX(- Math.PI / 2);

        disposeables.push(geometry);

        var vertices = geometry.attributes.position.array;

        //console.log('vertices', vertices);
        for (var i = 0, j = 0, l = vertices.length; i < l; i++, j += 3) {
            vertices[j + 1] = data[i];
        }

        //console.log('controls.target', controls.target);
        //console.log('camera.position', camera.position);
        //console.log('vergices', vertices);

        geometry.computeFaceNormals(); // needed for helper

        //

        texture = terrainData.imageUrl ? generateTexture(terrainData.imageUrl) :
            new THREE.CanvasTexture(generateTexture2(data, arrayWidth, arrayDepth));
        texture.wrapS = THREE.ClampToEdgeWrapping;
        texture.wrapT = THREE.ClampToEdgeWrapping;

        disposeables.push(texture);

        mesh = new THREE.Mesh(geometry, new THREE.MeshBasicMaterial({ map: texture }));
        scene.add(mesh);

        var cone = new THREE.ConeBufferGeometry(1, 4, 3);

        disposeables.push(cone);

        cone.translate(0, -2, 0);
        cone.rotateX(-Math.PI / 2);
        helper = new THREE.Mesh(cone, new THREE.MeshNormalMaterial());
        scene.add(helper);


        // UI
        $info = $("<div>")
            .addClass('webgis-threed-info-holder')
            .css({})
            .appendTo($(container));
        $infoSingleResults = $("<div>")
            .appendTo($info);

        $infoResult = $("<div><table></table></div>")
            .css('display', 'none')
            .addClass('box webgis-threed-info-result')
            .appendTo($info);

        $("<div>")
            .addClass('remove-all')
            .text('Alle entfernen')
            .appendTo($infoResult)
            .click(function (e) {
                e.stopPropagation();
                e.originalEvent.preventDefault();

                for (var s in clickSpheres) {
                    scene.remove(clickSpheres[s]);
                }
                for (var a in clickArrows) {
                    scene.remove(clickArrows[a]);
                }
                clickSpheres = [];
                clickArrows = [];

                $infoSingleResults.empty();
                calcResults();
            });


        $infoCurrent = $("<div>")
            .addClass('box webgis-threed-info-current')
            .appendTo($info);
        $("<table class='current'><tr><td>"+webgis.l10n.get("easting")+":</td><td class='current-x'></td></tr><tr><td>"+webgis.l10n.get("northing")+":</td><td class='current-z'></td></tr><tr><td>Höhe:</td><td class='current-y'></td></tr></table>")
            .appendTo($infoCurrent);

        container.addEventListener('mousemove', onMouseMove, false);
        container.addEventListener('pointerdown', onMouseDown, false);
        container.addEventListener('pointerup', onMouseUp, false);

        window.addEventListener('resize', onWindowResize, false);
    }

    this.dispose = function() {
        console.log('disposse scene');

        terrainData = null;

        renderer.dispose();
        renderer = null;
        scene = null;

        for (var d in disposeables) {
            //console.log('dispose', disposeables[d]);
            disposeables[d].dispose();
        }
        disposeables = [];
    };

    function onWindowResize() {

        camera.aspect = window.innerWidth / window.innerHeight;
        camera.updateProjectionMatrix();

        renderer.setSize(window.innerWidth, window.innerHeight);

    }

    function generateTexture(imageUrl) {
        return new THREE.TextureLoader().load(imageUrl);
    }

    function generateTexture2(data, width, height) {

        // bake lighting into texture

        var canvas, canvasScaled, context, image, imageData, vector3, sun, shade;

        vector3 = new THREE.Vector3(0, 0, 0);

        sun = new THREE.Vector3(1, 1, 1);
        sun.normalize();

        canvas = document.createElement('canvas');
        canvas.width = width;
        canvas.height = height;

        context = canvas.getContext('2d');
        context.fillStyle = '#000';
        context.fillRect(0, 0, width, height);

        image = context.getImageData(0, 0, canvas.width, canvas.height);
        imageData = image.data;

        var meanData = 0.0, n = 0, minData = 1e10, maxData = 1e-10;
        for (var i = 0; i < data.length; i++) {
            if (!data[i] || data[i] === 0) {
                continue;
            }
            n++;
            minData = Math.min(minData, data[i]);
            maxData = Math.max(maxData, data[i]);
        }
        for (var i = 0; i < data.length; i++) {
            if (!data[i] || data[i] === 0) {
                continue;
            }
            meanData += (data[i]) / n;
        }

        console.log('meandata', meanData);
        var fac = 1.0 / (maxData - minData);

        for (var i = 0, j = 0, l = imageData.length; i < l; i += 4, j++) {

            vector3.x = data[j - 2] - data[j + 2];
            vector3.y = 2;
            vector3.z = data[j - width * 2] - data[j + width * 2];
            vector3.normalize();

            shade = vector3.dot(sun);

            var pFac = 1.0 - (0.5 + (data[j] - meanData) * fac) / 2;  // macht result ein bisserl plastischer

            imageData[i] = (96 + shade * 128)    * pFac;
            imageData[i + 1] = (32 + shade * 96) * pFac;
            imageData[i + 2] = (shade * 96)      * pFac;
        }

        context.putImageData(image, 0, 0);

        // Scaled 4x

        canvasScaled = document.createElement('canvas');
        canvasScaled.width = width * 4;
        canvasScaled.height = height * 4;

        context = canvasScaled.getContext('2d');
        context.scale(4, 4);
        context.drawImage(canvas, 0, 0);

        image = context.getImageData(0, 0, canvasScaled.width, canvasScaled.height);
        imageData = image.data;

        //for (var i = 0, l = imageData.length; i < l; i += 4) {

        //    var v = ~ ~(Math.random() * 5);

        //    imageData[i] += v;
        //    imageData[i + 1] += v;
        //    imageData[i + 2] += v;

        //}

        context.putImageData(image, 0, 0);

        return canvasScaled;

    }

    //

    function animate() {
        if (renderer) {
            requestAnimationFrame(animate);

            render();
        }
    }

    function render() {
        if (renderer) {
            renderer.render(scene, camera);
        }
    }

    function onMouseMove(event) {

        mouse.x = (event.layerX / renderer.domElement.clientWidth) * 2 - 1;
        mouse.y = - (event.layerY / renderer.domElement.clientHeight) * 2 + 1;
        raycaster.setFromCamera(mouse, camera);

        // See if the ray from the camera into the world hits one of our meshes
        var intersects = raycaster.intersectObject(mesh);

        // Toggle rotation bool for meshes that we clicked
        if (intersects.length > 0) {

            $infoCurrent.find('.current-x').text(round(falseEasting + intersects[0].point.x));
            $infoCurrent.find('.current-y').text(round(intersects[0].point.y));
            $infoCurrent.find('.current-z').text(round(falseNorthing - intersects[0].point.z));

            helper.position.set(0, 0, 0);
            helper.lookAt(intersects[0].face.normal);

            helper.position.copy(intersects[0].point);
        }
    }

    var _lastLayerX, _lastLayerY;
    function onMouseDown(event) {
        _lastLayerX = event.layerX;
        _lastLayerY = event.layerY;
    }

    function onMouseUp(event) {

        if (_lastLayerX != event.layerX || _lastLayerY != event.layerY)
            return;

        //console.log('onMouseDown', event);
        mouse.x = (event.layerX / renderer.domElement.clientWidth) * 2 - 1;
        mouse.y = - (event.layerY / renderer.domElement.clientHeight) * 2 + 1;
        raycaster.setFromCamera(mouse, camera);

        // See if the ray from the camera into the world hits one of our meshes
        var intersects = raycaster.intersectObject(mesh);

        if (intersects.length > 0) {

            if (event.button === 0) {  // left mouse button
                var $last = $infoSingleResults.children('.webgis-threed-info-box').last();
               
                var h = intersects[0].point.y, dh, length3d, length2d;

                var sphere = new THREE.SphereBufferGeometry(.3, 32, 32);
                sphere.translate(intersects[0].point.x, intersects[0].point.y, intersects[0].point.z);
                var sphereMesh = new THREE.Mesh(sphere, new THREE.MeshNormalMaterial());
                scene.add(sphereMesh);

                clickSpheres.push(sphereMesh);

                if ($last.length === 1) {
                    var p = $last.data('p');

                    var dh = h - p[1];

                    var origin = new THREE.Vector3(p[0], p[1], p[2]);
                    var dir = new THREE.Vector3(
                        intersects[0].point.x - p[0],
                        intersects[0].point.y - p[1],
                        intersects[0].point.z - p[2]);
                    dir.normalize();

                    length3d = Math.sqrt(
                        (intersects[0].point.x - p[0]) * (intersects[0].point.x - p[0]) +
                        (intersects[0].point.y - p[1]) * (intersects[0].point.y - p[1]) +
                        (intersects[0].point.z - p[2]) * (intersects[0].point.z - p[2])
                    );

                    length2d = Math.sqrt(
                        (intersects[0].point.x - p[0]) * (intersects[0].point.x - p[0]) +
                        (intersects[0].point.z - p[2]) * (intersects[0].point.z - p[2])
                    );

                    var arrowHelper = new THREE.ArrowHelper(dir, origin, length3d, 0xff0000, 3);
                    scene.add(arrowHelper);

                    clickArrows.push(arrowHelper);
                }

                var $infobox = $("<div>")
                    .addClass('box webgis-threed-info-box')
                    .data('p', [intersects[0].point.x, intersects[0].point.y, intersects[0].point.z])
                    .appendTo($infoSingleResults);
                var $title = $("<div>")
                    .addClass('webgis-threed-info-box-title')
                    .appendTo($infobox);
                $("<div>")
                    .addClass('webgis-threed-info-box-undo')
                    .text("Undo")
                    .appendTo($title)
                    .click(function (e) {
                        e.stopPropagation();
                        e.originalEvent.preventDefault();

                        if (clickSpheres.length > 0) {
                            scene.remove(clickSpheres[clickSpheres.length - 1]);
                        }
                        if (clickArrows.length > 0) {
                            scene.remove(clickArrows[clickArrows.length - 1]);
                        }
                        clickSpheres.pop();
                        clickArrows.pop();

                        $(this).closest('.webgis-threed-info-box').remove();

                        calcResults();
                    });

                var $table = $("<table>").appendTo($infobox);
                $("<tr><td>Höhenwert:</td><td>" + round(h) + " m</td><tr>").appendTo($table);
                if (length3d) {
                    $("<tr><td>Länge 3D:</td><td>" + round(length3d) + " m</td><tr>").appendTo($table);
                    $infobox.data('length3d', length3d);
                }
                if (length2d) {
                    $("<tr><td>Länge 2D:</td><td>" + round(length2d) + " m</td><tr>").appendTo($table);
                    $infobox.data('length2d', length2d);
                }
                if (dh) {
                    $("<tr><td>delta H:</td><td>" + round(dh) + " m</td><tr>").appendTo($table);
                    $infobox.data('dh', dh);
                }

                calcResults();
            }

            if (event.button === 2) {  // right mouse button
                //console.log('reset target', intersects[0].point);

                controls.target.x = intersects[0].point.x;
                controls.target.y = intersects[0].point.y + 2.0;
                controls.target.z = intersects[0].point.z;

                var vx = camera.position.x - controls.target.x,
                    vy = camera.position.y - controls.target.y,
                    vz = camera.position.z - controls.target.z;

                camera.position.x = controls.target.x + vx / 2.0;
                camera.position.y = controls.target.y + vy / 2.0;
                camera.position.z = controls.target.z + vz / 2.0;

                // update Camera
                controls.update();
                
            }
        }
    }

    function round(v) {
        return Math.round(v * 100) / 100;
    }

    function calcResults() {
        $infoResult.children('table').empty();

        var length3d=0, length2d=0, dh=0;
        $infoSingleResults.children().each(function (e, info) {
            var $info = $(info);

            if ($info.data('length3d'))
                length3d += $info.data('length3d');
            if ($info.data('length2d'))
                length2d += $info.data('length2d');
            if ($info.data('dh'))
                dh += $info.data('dh');
        });

        if (length3d || length2d || dh) {
            var $table = $infoResult.children('table');
            if (length3d) {
                $("<tr><td>&#8721; Länge 3D:</td><td>" + round(length3d) + " m</td><tr>").appendTo($table);
            }
            if (length2d) {
                $("<tr><td>&#8721; Länge 2D:</td><td>" + round(length2d) + " m</td><tr>").appendTo($table);
            }
            if (dh) {
                $("<tr><td>&#8721; delta H:</td><td>" + round(dh) + " m</td><tr>").appendTo($table);
            }

            $infoResult.css('display', '');
        } else {
            $infoResult.css('display', 'none');
        }
    };
};