package main

import (
	"fmt"
	"io/ioutil"
	"log"
	"net/http"
	"strings"
)

func sayhelloName(w http.ResponseWriter, r *http.Request) {
	r.ParseForm()       // parse arguments, you have to call this by yourself
	fmt.Println(r.Form) // print form information in server side
	fmt.Println("path", r.URL.Path)
	fmt.Println("scheme", r.URL.Scheme)
	fmt.Println(r.Form["url_long"])
	for k, v := range r.Form {
		fmt.Println("key:", k)
		fmt.Println("val:", strings.Join(v, ""))
	}

	downloadEndpoint := "/getFileByTitle"
	if strings.HasPrefix(r.URL.Path, downloadEndpoint) {
		filename := (r.URL.Path)[len(downloadEndpoint)+1:]
		file := getFileByTitle(filename)
		w.Write(file)

	} else {
		fmt.Fprintf(w, "Hello astaxie!")
	} // send data to client side
}

func main() {
	http.HandleFunc("/", sayhelloName)       // set router
	err := http.ListenAndServe(":9090", nil) // set listen port
	if err != nil {
		log.Fatal("ListenAndServe: ", err)
	}
}

func getFileByTitle(title string) []byte {
	data, err := ioutil.ReadFile("../Output/" + title)
	if err != nil {
		panic(err)
	}
	return data
}
