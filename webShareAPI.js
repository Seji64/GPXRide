IsShareSupported = () => {
    if (navigator.share) {
        return true;
    } else {
        return false;
    }
};

ShareUrl = (title, text, url) => {
    if (navigator.share) {
        navigator.share({
            title: title,
            text: text,
            url: url,
        });
    }
}

function dataURLtoFile(dataurl, filename, filetype) {

    var arr = dataurl.split(','),
        mime = arr[0].match(/:(.*?);/)[1],
        bstr = atob(arr[1]),
        n = bstr.length,
        u8arr = new Uint8Array(n);

    while (n--) {
        u8arr[n] = bstr.charCodeAt(n);
    }

    return new File([u8arr], filename, { type: filetype });
}

CanShareThisFile = (fileName, fileType, fileData) => {
    if (navigator.share)
    {
        const file = dataURLtoFile(fileData, fileName, fileType)
        if (navigator.canShare(file)) {
            return true;
        } else { return false; }
    }
}

ShareFile = (title, text, fileName, fileType, fileData) => {
    if (navigator.share) {
        var result = true;
        const file = dataURLtoFile(fileData, fileName, fileType)
        return navigator.share({
            title: title,
            text: text,
            files: [file],
        });
    }
}