$(function () {
    var responsiveDesignPageController = (function ($) {
        // MOUSEOVEREVENTS / MOUSELEAVEEVENTS
        var $body = $("body");
        
        var $hoverLinks = $(".hoverLink");
        var $productHeadline = $("#productHeadline");
        var $productLink = $(".productLink");
        var $productLinkMouseOverElements = $productLink.find(".ImageContainer, .ProductCaption");
        $productHeadline.mouseover(function () {
            $productLinkMouseOverElements
            .addClass("mouseoverStyle");
        })
        .mouseleave(function () {
            $productLinkMouseOverElements
            .removeClass("mouseoverStyle");
        });

        $hoverLinks.mouseover(function () {
            $(this).removeClass("menuPlaceHolderMenuElementMouseLeave").addClass("menuPlaceHolderMenuElementMouseOver");
        })
        .mouseleave(function () {
            $(this).removeClass("menuPlaceHolderMenuElementMouseOver").addClass("menuPlaceHolderMenuElementMouseLeave");
        });

        $body.on("mouseover", ".productLink", function () {
            $(this).find(".ImageContainer, .ProductCaption")
            .addClass("mouseoverStyle");
        })
        .on("mouseleave", ".productLink", function () {
            $(this).find(".ImageContainer, .ProductCaption")
            .removeClass("mouseoverStyle");
        });

        var $menuPlaceHolderMenu = $("#menuPlaceHolderMenu");
        $body.on("mouseover", "#menuPlaceHolderImage", function () {
            $(this)
                .addClass("mouseoverStyle")
                .addClass("mouseoverMenuPlaceHolderStyle");

        })
        .on("mouseleave", "#menuPlaceHolderImage", function () {
            $(this)
                .removeClass("mouseoverStyle")
                .removeClass("mouseoverMenuPlaceHolderStyle");
        });

        $body.on("mouseover", ".menuPlaceHolderMenuElement", function () {
            $(this).removeClass("menuPlaceHolderMenuElementMouseLeave").addClass("menuPlaceHolderMenuElementMouseOver");
        })
        .on("mouseleave", ".menuPlaceHolderMenuElement", function () {
            $(this).removeClass("menuPlaceHolderMenuElementMouseOver").addClass("menuPlaceHolderMenuElementMouseLeave");
        });

        var $mobileLink = $(".mobileLink");
        var $tabletLink = $(".tabletLink");
        var $desktopLink = $(".desktopLink");
        $mobileLink.mouseover(function () {
            $(".mobileLink").addClass("mouseoverStyle");
        })
        .mouseleave(function () {
            $(".mobileLink").removeClass("mouseoverStyle");
        });
        $tabletLink.mouseover(function () {
            $(".tabletLink").addClass("mouseoverStyle");
        })
        .mouseleave(function () {
            $(".tabletLink").removeClass("mouseoverStyle");
        });
        $desktopLink.mouseover(function () {
            $(".desktopLink").addClass("mouseoverStyle");
        })
        .mouseleave(function () {
            $(".desktopLink").removeClass("mouseoverStyle");
        });

        // CLICKEVENTS
        var $menuPlaceHolderImage = $("#menuPlaceHolderImage");
        $menuPlaceHolderImage.click(function () {
            if ($menuPlaceHolderMenu.hasClass("hidden"))
                $menuPlaceHolderMenu.show().removeClass("hidden");
            else
                $menuPlaceHolderMenu.hide().addClass("hidden");
        });

        // RESPONSIVE STUFF
        var $window = $(window);
        var $pageHeader = $("#pageHeader");
        var $productHeadlineContainer = $("#productHeadlineContainer");
        var $menuListItems = $(".menuListItem");
        var $eLogo = $("#eLogo");
        var $eLogoDefaultHeight = $eLogo.height();
        var $eLogoDefaultWidth = $eLogo.width();
        var eLogoSquareDimension = 90;
        var headerDefaultBottomBorderStyle = $pageHeader.css("border-bottom");
        var $menuPlaceHolderContainer = $("#menuPlaceHolderContainer");
        var $pageFooter = $("#pageFooter");
        var $responsivePageFooter = $("#responsivePageFooter");
        var $fullSizeContainer = $("#fullSizeContainer");
        var $underFullSizeContainer = $("#underFullSizeContainer");
        var $responsiveProductTable = $("#responsiveProductTable");
        var $responsiveSmallProductListing = $("#responsiveSmallProductListing");
        var responsiveMenuContent = "";
        $window.resize(function () {
            var $windowHeight = $window.height();
            var $windowWidth = $window.width();
            if (($windowHeight < 801) || ($windowWidth < 1251)) {
                $productHeadlineContainer.hide();

                $pageHeader.css("border-bottom", "none");
                $menuListItems.hide();
                $eLogo.css("height", eLogoSquareDimension + "px");
                $eLogo.css("width", eLogoSquareDimension + "px");
                $menuPlaceHolderContainer.show();

                $menuPlaceHolderMenu.show();

                if (responsiveMenuContent === "") {
                    $menuPlaceHolderMenu.empty();
                    $(".menuListItem").each(function (index) {
                        var elementHtml = $(this).find("a").clone().addClass("menuPlaceHolderMenuElement").wrap('<div></div>').parent().html();
                        if (typeof elementHtml != 'undefined')
                            responsiveMenuContent += "<div class='menuListItem responsiveMenuElementContainer'>" + elementHtml + "</div>";
                    });
                    $menuPlaceHolderMenu.append(responsiveMenuContent);
                }

                $menuPlaceHolderMenu.hide().addClass("hidden");

                $pageFooter.hide();
                $responsivePageFooter.show();
                $fullSizeContainer.hide()
                $underFullSizeContainer.hide()

                $responsiveProductTable.show();
            }
            else {
                $productHeadlineContainer.show();
                $pageHeader.css("border-bottom", headerDefaultBottomBorderStyle);
                $menuListItems.show();
                $eLogo.css("height", $eLogoDefaultHeight + "px");
                $eLogo.css("width", $eLogoDefaultWidth + "px");
                $menuPlaceHolderContainer.hide();

                $menuPlaceHolderMenu.hide();

                $pageFooter.show();
                $responsivePageFooter.hide();
                $fullSizeContainer.show();
                $underFullSizeContainer.show();

                $responsiveProductTable.hide();
            }
            setTimeout(function () {
                if ($windowWidth < 631) {
                    $responsiveProductTable.hide();
                    $fullSizeContainer.hide()
                    $underFullSizeContainer.hide()
                    $responsiveSmallProductListing.show();
                }
                else if (($windowWidth > 630) && ($windowWidth < 1251) || ($windowHeight < 801)) {
                    $responsiveProductTable.show();
                    $fullSizeContainer.hide()
                    $underFullSizeContainer.hide()
                    $responsiveSmallProductListing.hide();
                }
                else if (($windowWidth > 1250) && ($windowHeight > 800)) {
                    $responsiveProductTable.hide();
                    $fullSizeContainer.show()
                    $underFullSizeContainer.show()
                    $responsiveSmallProductListing.hide();
                }
                else {
                    $responsiveProductTable.hide();
                    $fullSizeContainer.show()
                    $underFullSizeContainer.show()
                    $responsiveSmallProductListing.hide();
                }
            }, 10);
        });
        setTimeout(function () {
            $window.resize();
            $menuPlaceHolderMenu.hide().addClass("hidden");
        }, 1);
    })(jQuery);
});