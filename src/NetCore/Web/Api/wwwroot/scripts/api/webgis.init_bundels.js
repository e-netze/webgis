webgis.cryptoJS = CryptoJS;

webgis.cryptoJS.simpleEcrypt = function (key, value) {
    var encrypted = webgis.cryptoJS.AES ? webgis.cryptoJS.AES.encrypt(value, key) : value;
    return encrypted;
}
webgis.cryptoJS.simpleDecrypt = function (key, value) {
    var decrypted = webgis.cryptoJS.AES ? webgis.cryptoJS.AES.decrypt(value, key).toString(webgis.cryptoJS.enc.Utf8) : value;
    return decrypted;
}