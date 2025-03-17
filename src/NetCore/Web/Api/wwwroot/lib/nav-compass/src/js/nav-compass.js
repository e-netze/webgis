var navCompass = function(targetElement) {

    targetElement.innerHTML =
    `<div class="nav-compass">
        <div class="nav-compass-arrow"></div>
        <div class="nav-compass-circle"></div>
        <div class="nav-compass-current-position"></div>
        <div class="nav-compass-target-direction"></div>
    </div>

    <div class="nav-compass-target-info">
        <div class="nav-compass-target-info-arrow">

        </div>
        <div class="nav-compass-target-info-text">
            <div class="nav-compass-target-info-label">
            </div>
            <div class="nav-compass-target-info-distance">
            </div>
        </div>
    </div>`;

    const compassCircle = document.querySelector(".nav-compass-circle");
    const myPoint = document.querySelector(".nav-compass-current-position");
    const targetDirection = document.querySelector('.nav-compass-target-direction');
    const targetDistanceElement = document.querySelector('.nav-compass-target-info-distance');
    const infoArrow = document.querySelector(".nav-compass-target-info-arrow");

    const me = this;

    let compass;
    let targetPoint;
    let isStarted = false;
    
    const isIOS = (
        navigator.userAgent.match(/(iPod|iPhone|iPad)/) &&
        navigator.userAgent.match(/AppleWebKit/)
    );

    this.start = function(navPoint, label) {
        
        targetPoint=navPoint;

        if(label) {
            document.querySelector('.nav-compass-target-info-label').innerHTML=label;
        }

        if (isIOS) {
          DeviceOrientationEvent.requestPermission()
            .then((response) => {
              if (response === "granted") {
                window.addEventListener("deviceorientation", orientationHandler, true);
              } else {
                alert("has to be allowed!");
              }
            })
            .catch(() => alert("not supported"));
        } else {
          window.addEventListener("deviceorientationabsolute", orientationHandler, true);
        }

        isStarted = true;

        if(targetPoint) {
            navigator.geolocation.getCurrentPosition(locationHandler);
        }
    }

    this.stop = function() {
        isStarted = false;

        if(isIOS) {
            window.addEventListener("deviceorientation", orientationHandler);
        } else {
            window.removeEventListener("deviceorientationabsolute", orientationHandler);
        }
    };

    this.onPositionChanged = null;

    function orientationHandler(e) {
        compass = e.webkitCompassHeading || Math.abs(e.alpha - 360);
        compassCircle.style.transform = `translate(-50%, -50%) rotate(${ -compass }deg)`;

        if(pointDegree !== null) {
            if (calcAbsoluteAngleDifference(pointDegree, compass) > 15) {
               myPoint.style.opacity = 0;
            } else if (pointDegree) {
               myPoint.style.opacity = 1;
            }

            targetDirection.style.transform = `translate(-50%, -50%) rotate(${ -compass + pointDegree }deg)`;
            //infoArrow.style.transform=`rotate(${  -compass + pointDegree }deg)`;
        }
    }

    let pointDegree = null;
    let pointDistance = null;

    function locationHandler(position) {
        //console.log('receive position', position);

        const { latitude, longitude } = position.coords;
        pointDegree = calcDegreeToPoint(latitude, longitude);
        pointDistance = calcSphericDistance(latitude, longitude);

        //console.log('pointDistance', pointDistance);
        targetDistanceElement.innerHTML=`${ Math.round(pointDistance) }m (+/- ${ Math.round(position.coords.accuracy) }m)`

        if (pointDegree < 0) {
            pointDegree = pointDegree + 360;
        }

        targetDirection.style.opacity = 1;

        if(me.onPositionChanged) {
            me.onPositionChanged(position, compass, pointDegree);
        }

        if(isStarted) {

            setTimeout(function(){
                navigator.geolocation.getCurrentPosition(locationHandler);
            },1000);

        }
    }

    function calcDegreeToPoint(latitude, longitude) {
        const phiK = (targetPoint.lat * Math.PI) / 180.0;
        const lambdaK = (targetPoint.lng * Math.PI) / 180.0;
        const phi = (latitude * Math.PI) / 180.0;
        const lambda = (longitude * Math.PI) / 180.0;
        const psi =
            (180.0 / Math.PI) *
            Math.atan2(
            Math.sin(lambdaK - lambda),
            Math.cos(phi) * Math.tan(phiK) -
                Math.sin(phi) * Math.cos(lambdaK - lambda)
            );

        //console.log('psi', psi);
        return Math.round(psi);
    }

    function calcSphericDistance(latitude, longitude) {
        var lat1 = latitude, lng1 = longitude;
        var lat2 = targetPoint.lat, lng2 = targetPoint.lng;

        var R = 6378137; // Radius of the earth in m
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLon = (lng2 - lng1) * Math.PI / 180.0;
        var a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
            Math.cos((lat1) * Math.PI / 180.0) * Math.cos((lat2) * Math.PI / 180.0) *
                Math.sin(dLon / 2) * Math.sin(dLon / 2);
        var c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
        var d = R * c; // Distance in m
        return d;
    };

    function calcAbsoluteAngleDifference(a1, a2) {
        a1 = a1 * Math.PI / 180.0;
        a2 = a2 * Math.PI / 180.0;

        var v1 = [ Math.cos(a1), Math.sin(a1) ];
        var v2 = [ Math.cos(a2), Math.sin(a2) ];

        var cos_a = v1[0]*v2[0]+v1[1]*v2[1];

        return Math.abs(Math.acos(cos_a)*180.0/Math.PI);
    };
};