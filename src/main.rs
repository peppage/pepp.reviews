#[macro_use]
extern crate actix_web;

#[macro_use]
extern crate serde_json;

use actix_http::{body::Body, Response};
use actix_web::dev::ServiceResponse;
use actix_web::http::StatusCode;
use actix_web::middleware::errhandlers::{ErrorHandlerResponse, ErrorHandlers};
use actix_web::{web, App, HttpResponse, HttpServer, Result};

use atom_syndication::Feed;
use handlebars::Handlebars;
use serde::ser::{Serialize, SerializeStruct, Serializer};

use std::error::Error;
use std::io;

struct Video {
    id: String,
    title: String,
    url: String,
}

impl Serialize for Video {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: Serializer,
    {
        let mut s = serializer.serialize_struct("video", 3)?;
        s.serialize_field("id", &self.id)?;
        s.serialize_field("title", &self.title)?;
        s.serialize_field("url", &self.url)?;
        s.end()
    }
}

#[get("/")]
async fn index(hb: web::Data<Handlebars<'_>>) -> HttpResponse {
    let mut videos: Vec<Video> = Vec::new();

    let feed = match get_videos().await {
        Ok(f) => f,
        Err(error) => panic!("failed to get feed: {:?}", error),
    };

    for entry in feed.entries() {
        let link = entry.links().first().unwrap();
        let youtube = entry.extensions().get("yt").unwrap();
        let id = match youtube.get("videoId").unwrap().first().unwrap().value() {
            Some(i) => i,
            None => "",
        };

        let video = Video {
            id: id.to_owned(),
            title: entry.title().to_owned(),
            url: link.href().to_owned(),
        };

        videos.push(video);
    }

    let data = json!({ "videos": videos });
    let body = hb.render("index", &data).unwrap();

    HttpResponse::Ok().body(body)
}

#[actix_web::main]
async fn main() -> io::Result<()> {
    // Handlebars uses a repository for the compiled templates. This object must be
    // shared between the application threads, and is therefore passed to the
    // Application Builder as an atomic reference-counted pointer.
    let mut handlebars = Handlebars::new();
    handlebars
        .register_templates_directory(".hbs", "./src/static/templates")
        .unwrap();
    let handlebars_ref = web::Data::new(handlebars);

    HttpServer::new(move || {
        App::new()
            .wrap(error_handlers())
            .app_data(handlebars_ref.clone())
            .service(index)
    })
    .bind("127.0.0.1:8080")?
    .run()
    .await
}

// Custom error handlers, to return HTML responses when an error occurs.
fn error_handlers() -> ErrorHandlers<Body> {
    ErrorHandlers::new().handler(StatusCode::NOT_FOUND, not_found)
}

// Error handler for a 404 Page not found error.
fn not_found<B>(res: ServiceResponse<B>) -> Result<ErrorHandlerResponse<B>> {
    let response = get_error_response(&res, "Page not found");
    Ok(ErrorHandlerResponse::Response(
        res.into_response(response.into_body()),
    ))
}

// Generic error handler.
fn get_error_response<B>(res: &ServiceResponse<B>, error: &str) -> Response<Body> {
    let request = res.request();

    // Provide a fallback to a simple plain text response in case an error occurs during the
    // rendering of the error page.
    let fallback = |e: &str| {
        Response::build(res.status())
            .content_type("text/plain")
            .body(e.to_string())
    };

    let hb = request
        .app_data::<web::Data<Handlebars>>()
        .map(|t| t.get_ref());
    match hb {
        Some(hb) => {
            let data = json!({
                "error": error,
                "status_code": res.status().as_str()
            });
            let body = hb.render("error", &data);

            match body {
                Ok(body) => Response::build(res.status())
                    .content_type("text/html")
                    .body(body),
                Err(_) => fallback(error),
            }
        }
        None => fallback(error),
    }
}

async fn get_videos() -> Result<Feed, Box<dyn Error>> {
    let content = reqwest::get(
        "https://www.youtube.com/feeds/videos.xml?channel_id=UCmkCPjKpngDHZp0orr7GuGA",
    )
    .await?
    .bytes()
    .await?;
    let channel = Feed::read_from(&content[..])?;
    Ok(channel)
}
