import { Component, Inject } from '@angular/core';
import 'rxjs/Rx'
import {saveAs as importedSaveAs} from "file-saver";
import { Http, Response, RequestOptions, RequestOptionsArgs, ResponseContentType } from '@angular/http';

@Component({
    selector: 'home',
    templateUrl: './home.component.html'
})
export class HomeComponent {
    public currentCount = 0;
    downloading = false;
    urls: string[] = [];
    http: Http;
    baseUrl: string;
    thumbnails: FileInformation[] = [];
    loaded = false;
    blobs: Blob[];
    
    constructor(http: Http, @Inject('BASE_URL') baseUrl: string) {        
        this.http = http;
        this.baseUrl = baseUrl;
        this.blobs = [] as Blob[];
    }


    public addUrls() {
        const container = Array.from(document.getElementById('input-container')!.children);
        container.forEach(
            e => {
                const el = (<HTMLInputElement>e).value
                if (el !== "")   this.urls.push(el);
            }
        );
        console.log(this.urls)
    }
    public sendUrls() {
        this.addUrls();
        this.http.post(`${this.baseUrl}api/YtDl/loadURLs`, this.urls).subscribe(
            result => {
                this.downloading = true;
                try {
                    this.thumbnails = Array.from(result.json() as FileInformation[]).map(el => {
                        return (el as FileInformation);
                    });
                    console.log(this.thumbnails);
                    this.loaded = true;
                }
                catch (e){
                    console.error(e);
                    const s = result.toString();
                    console.log(s);
                }
            }, 
            error => console.error(error));
    }
    public download(id: string, tittle: string) {
        let options = new RequestOptions({responseType: ResponseContentType.ArrayBuffer });
        this.http.get(`${this.baseUrl}api/YtDl/downloadFullVideo?id=${id}`).subscribe(
            data => {
                console.log('downloaded')
            },
            error => {
                console.log('err')
                console.error(error);
            },

        );
    }

    
}
interface FileInformation {
    tittle: string;
    id: string;
    thumbnails: Thumbnail;
}
interface Thumbnail {
    highResUrl: string;
    lowResUrl: string;
    maxResUrl: string;
    mediumResUrl: string;
    standardResUrl: string;
}