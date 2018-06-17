import { Component, Inject } from '@angular/core';
import 'file-loader';
import { Http, Response } from '@angular/http';
import 'rxjs/Rx' ;

@Component({
    selector: 'playlists',
    templateUrl: './playlists.component.html'
})
export class PlaylistComponent {
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
    public sendUrl() {
        this.addUrls();
        this.http.get(`${this.baseUrl}api/YtDl/loadPlaylist?url=${this.urls[0]}`).subscribe(
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
        this.http.get(`${this.baseUrl}api/YtDl/Download?id=${id}`)
        .map( (x: Response) => x.json() )
        .subscribe(
            data => {
                console.log('Downloading: ', data)
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
