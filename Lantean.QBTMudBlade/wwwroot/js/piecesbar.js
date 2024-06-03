/*
 * Bittorrent Client using Qt and libtorrent.
 * Copyright (C) 2022  Jesse Smick <jesse.smick@gmail.com>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 *
 * In addition, as a special exception, the copyright holders give permission to
 * link this program with the OpenSSL project's "OpenSSL" library (or with
 * modified versions of it that use the same license as the "OpenSSL" library),
 * and distribute the linked executables. You must obey the GNU General Public
 * License in all respects for all of the code used other than "OpenSSL".  If you
 * modify file(s), you may extend this exception to your version of the file(s),
 * but you are not obligated to do so. If you do not wish to do so, delete this
 * exception statement from your version.
 */

'use strict';

if (window.qbt === undefined) {
    window.qbt = {};
}

window.qbt.piecesBarUniqueId = 0;

class PiecesBar {
    STATUS_DOWNLOADING = 1;
    STATUS_DOWNLOADED = 2;

    // absolute max width of 4096
    // this is to support all browsers for size of canvas elements
    // see https://github.com/jhildenbiddle/canvas-size#test-results
    MAX_CANVAS_WIDTH = 4096;

    constructor(pieces, parameters) {
        this.id = 'piecesbar_' + (window.qbt.piecesBarUniqueId++);
        this.width = 0;
        this.height = 0;
        this.downloadingColor = 'green';
        this.haveColor = 'blue';
        this.borderSize = 1;
        this.borderColor = '#999';

        if (parameters && (typeof(parameters) === 'object')) {
            Object.assign(this, parameters)
        }

        this.height = Math.max(this.height, 12);
        this.setPieces(pieces);
    }

    createElement() {
        this.obj = document.createElement('div');
        this.obj.className = 'piecesbarWrapper';
        this.obj.id = this.id;
        this.obj.style = 'border: ' + this.borderSize.toString() + 'px solid ' + this.borderColor + '; height: ' + this.height.toString() + 'px';

        this.canvas = document.createElement('canvas');
        this.canvas.id = this.id + '_canvas';
        this.canvas.className = 'piecesbarCanvas';
        this.canvas.width = (this.width - (2 * this.borderSize)).toString();

        this.obj.appendChild(this.canvas);

        if (this.width > 0) {
            this.setPieces(this.pieces);
        } else {
            setTimeout(() => { this.checkForParent(this.id); }, 1);
        }

        return this.obj;
    }

    clear() {
        this.setPieces([]);
    }

    setPieces(pieces) {
        if (!Array.isArray(pieces)) {
            this.pieces = [];
        } else {
            this.pieces = pieces;
        }
        this.refresh(true);
    }

    refresh(force) {
        if (!this.obj?.parentNode) {
            return;
        }

        const pieces = this.pieces;

        // if the number of pieces is small, use that for the width,
        // and have it stretch horizontally.
        // this also limits the ratio below to >= 1
        const width = Math.min(this.obj.offsetWidth, this.pieces.length, this.MAX_CANVAS_WIDTH);
        if ((this.width === width) && !force) {
            return;
        }

        this.width = width;

        // change canvas size to fit exactly in the space
        this.canvas.width = width - (2 * this.borderSize);

        const canvas = this.canvas;
        const ctx = canvas.getContext('2d');
        ctx.clearRect(0, 0, canvas.width, canvas.height);

        const imageWidth = canvas.width;

        if (imageWidth.length === 0)
            return;

        let minStatus = Infinity;
        let maxStatus = 0;

        for (const status of pieces) {
            if (status > maxStatus) {
                maxStatus = status;
            }

            if (status < minStatus) {
                minStatus = status;
            }
        }

        // if no progress then don't do anything
        if (maxStatus === 0) {
            return;
        }

        // if all pieces are downloaded, fill entire image at once
        if (minStatus === this.STATUS_DOWNLOADED) {
            ctx.fillStyle = this.haveColor;
            ctx.fillRect(0, 0, canvas.width, canvas.height);
            return;
        }

        /* Linear transformation from pieces to pixels.
         *
         * The canvas size can vary in width so this figures out what to draw at each pixel.
         * Inspired by the GUI code here https://github.com/qbittorrent/qBittorrent/blob/25b3f2d1a6b14f0fe098fb79a3d034607e52deae/src/gui/properties/downloadedpiecesbar.cpp#L54
         *
         * example ratio > 1 (at least 2 pieces per pixel)
         *        +-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+
         * pieces |  2  |  1  |  2  |  0  |  2  |  0  |  1  |  0  |  1  |  2  |
         *        +---------+---------+---------+---------+---------+---------+
         * pixels |         |         |         |         |         |         |
         *        +---------+---------+---------+---------+---------+---------+
         *
         * example ratio < 1 (at most 2 pieces per pixel)
         * This case shouldn't happen since the max pixels are limited to the number of pieces
         *        +---------+---------+---------+---------+----------+--------+
         * pieces |    2    |    1    |    1    |    0    |    2    |    2    |
         *        +-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+
         * pixels |     |     |     |     |     |     |     |     |     |     |
         *        +-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+
         */

        const ratio = pieces.length / imageWidth;

        let lastValue = null;
        let rectangleStart = 0;

        // for each pixel compute its status based on the pieces
        for (let x = 0; x < imageWidth; ++x) {
            // find positions in the pieces array
            const piecesFrom = x * ratio;
            const piecesTo = (x + 1) * ratio;
            const piecesToInt = Math.ceil(piecesTo);

            const statusValues = {
                [this.STATUS_DOWNLOADING]: 0,
                [this.STATUS_DOWNLOADED]: 0
            };

            // aggregate the status of each piece that contributes to this pixel
            for (let p = piecesFrom; p < piecesToInt; ++p) {
                const piece = Math.floor(p);
                const pieceStart = Math.max(piecesFrom, piece);
                const pieceEnd = Math.min(piece + 1, piecesTo);

                const amount = pieceEnd - pieceStart;
                const status = pieces[piece];

                if (status in statusValues)
                    statusValues[status] += amount;
            }

            // normalize to interval [0, 1]
            statusValues[this.STATUS_DOWNLOADING] /= ratio;
            statusValues[this.STATUS_DOWNLOADED] /= ratio;

            // floats accumulate small errors, so smooth it out by rounding to hundredths place
            // this effectively limits each status to a value 1 in 100
            statusValues[this.STATUS_DOWNLOADING] = Math.round(statusValues[this.STATUS_DOWNLOADING] * 100) / 100;
            statusValues[this.STATUS_DOWNLOADED] = Math.round(statusValues[this.STATUS_DOWNLOADED] * 100) / 100;

            // float precision sometimes _still_ gives > 1
            statusValues[this.STATUS_DOWNLOADING] = Math.min(statusValues[this.STATUS_DOWNLOADING], 1);
            statusValues[this.STATUS_DOWNLOADED] = Math.min(statusValues[this.STATUS_DOWNLOADED], 1);

            if (!lastValue) {
                lastValue = statusValues;
            }

            // group contiguous colors together and draw as a single rectangle
            if ((lastValue[this.STATUS_DOWNLOADING] === statusValues[this.STATUS_DOWNLOADING])
                && (lastValue[this.STATUS_DOWNLOADED] === statusValues[this.STATUS_DOWNLOADED])) {
                continue;
            }

            const rectangleWidth = x - rectangleStart;
            this._drawStatus(ctx, rectangleStart, rectangleWidth, lastValue);

            lastValue = statusValues;
            rectangleStart = x;
        }

        // fill a rect at the end of the canvas
        if (rectangleStart < imageWidth) {
            const rectangleWidth = imageWidth - rectangleStart;
            this._drawStatus(ctx, rectangleStart, rectangleWidth, lastValue);
        }
    }

    _drawStatus(ctx, start, width, statusValues) {
        // mix the colors by using transparency and a composite mode
        ctx.globalCompositeOperation = 'lighten';

        if (statusValues[this.STATUS_DOWNLOADING]) {
            ctx.globalAlpha = statusValues[this.STATUS_DOWNLOADING];
            ctx.fillStyle = this.downloadingColor;
            ctx.fillRect(start, 0, width, ctx.canvas.height);
        }

        if (statusValues[this.STATUS_DOWNLOADED]) {
            ctx.globalAlpha = statusValues[this.STATUS_DOWNLOADED];
            ctx.fillStyle = this.haveColor;
            ctx.fillRect(start, 0, width, ctx.canvas.height);
        }
    }

    checkForParent(id) {
        const obj = document.getElementById(id);
        if (!obj) {
            return;
        }
        if (!obj.parentNode) {
            return setTimeout(function () { checkForParent(id); }, 1);
        }

        this.refresh();
    }
}

window.qbt.PiecesBar = PiecesBar;

Object.freeze(window.qbt.PiecesBar);