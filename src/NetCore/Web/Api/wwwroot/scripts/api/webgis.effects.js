webgis.effects = new function ($) {
    var anim = function (item, style) {
        //console.log(style, item);
        $(item).addClass('webgis-' + style + '-animation');

        webgis.delayed(function (item) {
            $(item).addClass('webgis-animating-item').removeClass('webgis-' + style + '-animation');
        }, 1, item);

        webgis.delayed(function (item) {
            $(item).removeClass('webgis-animating-item');
        }, 300, item);
    };
    this.popup = function (item) {
        anim(item, 'popup');
    };
    this.newspaper = function (item) {
        anim(item, 'newspaper');
    };
    this.unfold3d = function (item) {
        anim(item, 'unfold3d');
    };
}(webgis.$ || jQuery);


webgis.swipeDetector = new function () {
    if (webgis.is_iOS) {
        document.addEventListener('touchstart', handleTouchStart, false);
        document.addEventListener('touchmove', handleTouchMove, false);
    }

    var xDown = null;
    var yDown = null;

    function getTouches(evt) {
        return evt.touches ||          // browser API
            evt.originalEvent.touches; // jQuery
    }

    function handleTouchStart(evt) {
        const firstTouch = getTouches(evt)[0];
        xDown = firstTouch.clientX;
        yDown = firstTouch.clientY;
    };

    function handleTouchMove(evt) {
        if (evt.target.id !== 'webgis-container' || !xDown || !yDown) {
            return;
        }

        var xUp = evt.touches[0].clientX;
        var yUp = evt.touches[0].clientY;

        var xDiff = xDown - xUp;
        var yDiff = yDown - yUp;

        if (Math.abs(xDiff) > Math.abs(yDiff)) {/*most significant*/
            if (xDiff > 0) {
                webgis.events.fire('on-ios-swipe', webgis, { swipe: 'left' });
            } else {
                webgis.events.fire('on-ios-swipe', webgis, { swipe: 'right' });
            }
        } else {
            if (yDiff > 0) {
                webgis.events.fire('on-ios-swipe', webgis, { swipe: 'up' });
            } else {
                webgis.events.fire('on-ios-swipe', webgis, { swipe: 'down' });
            }
        }
        /* reset values */
        xDown = null;
        yDown = null;
    };
};

//webgis.events.on('on-ios-swipe', function (channel, sender, args) {
//    console.log('swipe', args.swipe);
//});