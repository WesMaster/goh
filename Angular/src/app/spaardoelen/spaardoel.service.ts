import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { Spaardoel } from './spaardoel/spaardoel';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class SpaardoelService {

  private headers: HttpHeaders;
  private accessPointUrl: string = environment.api_url + 'spaardoel';

  constructor(private http: HttpClient)
  {
    this.headers = new HttpHeaders({'Content-Type': 'application/json; charset=utf-8'});
  }

  public async add(item)
  {
    return await this.http.post(this.accessPointUrl, item, {headers: this.headers}).toPromise();
  }

  public getAll(): Observable<Spaardoel[]>
  {
    return this.http.get<Spaardoel[]>(this.accessPointUrl, {headers: this.headers});
  }

  public get(id): Observable<Spaardoel>
  {
    return this.http.get<Spaardoel>(this.accessPointUrl + "/" + id, {headers: this.headers});
  }

  public async update(item)
  {
    return await this.http.put<Spaardoel>(this.accessPointUrl + "/" + item.id, item, {headers: this.headers}).toPromise();
  }

  public delete(id)
  {
    return this.http.delete(this.accessPointUrl + "/" + id, {headers: this.headers});
  }
}
