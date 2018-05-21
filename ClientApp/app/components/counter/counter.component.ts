import { Component, Inject } from '@angular/core';

import { Http } from '@angular/http';

@Component({
    selector: 'counter',
    templateUrl: './counter.component.html'
})
export class CounterComponent {
    public currentCount = 0;
    downloading = false;
    urls: string[] = [];
    http: Http;
    baseUrl: string;
    thumbnails: FileInformation[] = [];
    loaded = false;
    
    constructor(http: Http, @Inject('BASE_URL') baseUrl: string) {        
        this.http = http;
        this.baseUrl = baseUrl;
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
    public download(id: string) {
        this.http.get(`${this.baseUrl}api/YtDl/Download?id=${id}`).subscribe(
            result => {
                console.log('Downloading: ', id)
            },
            error => {console.error(error);}
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
