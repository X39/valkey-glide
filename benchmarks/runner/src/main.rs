use clap::*;

#[tokio::main]
async fn main() {
    let matches = command!()
        .arg(arg!(-v --verbose "Enable verbose output"))
        .subcommand(
            Command::new("run")
                .about("Run the benchmark")
                .arg(
                    arg!(--csharp "Run the C# benchmark")
                        .required(false)
                        .num_args(0),
                )
                .arg(
                    arg!(--java "Run the Java benchmark")
                        .required(false)
                        .num_args(0),
                )
                .arg(
                    arg!(--python "Run the Python benchmark")
                        .required(false)
                        .num_args(0),
                )
                .arg(
                    arg!(--rust "Run the Rust benchmark")
                        .required(false)
                        .num_args(0),
                )
                .arg(
                    arg!(--nodejs "Run the NodeJS benchmark")
                        .required(false)
                        .num_args(0),
                )
                .arg(
                    arg!(--go "Run the Go benchmark")
                        .required(false)
                        .num_args(0),
                )
                .subcommand(
                    Command::new("docker").about("Run the benchmark using docker containers"),
                )
                .subcommand(
                    Command::new("standalone")
                        .about("Run the benchmark against a live, standalone instance")
                        .arg(arg!(<HOST> "The host to connect to").required(true))
                        .arg(arg!([PORT] "The port to connect to").required(false)),
                ),
        );
    let matches = matches.get_matches();
    if let Some(run_matches) = matches.subcommand_matches("run") {
        command_run(run_matches).await;
    }
}

async fn command_run(matches: &ArgMatches) {
    if let Some(docker_matches) = matches.subcommand_matches("docker") {
        command_run_docker(matches, docker_matches).await;
    }
    if let Some(standalone_matches) = matches.subcommand_matches("standalone") {
        command_run_standalone(matches, standalone_matches).await;
    }
    panic!("No subcommand specified");
}

async fn command_run_docker(run_matches: &ArgMatches, docker_matches: &ArgMatches) -> Result<(), bollard::errors::Error> {
    let csharp = run_matches.get_flag("csharp");
    let java = run_matches.get_flag("java");
    let python = run_matches.get_flag("python");
    let rust = run_matches.get_flag("rust");
    let nodejs = run_matches.get_flag("nodejs");
    let go = run_matches.get_flag("go");
    let all = !csharp && !java && !python && !rust && !nodejs && !go;

    let docker = bollard::Docker::connect_with_local_defaults()?;
    docker.create_container(Some(bollard::container::CreateContainerOptions{

        ..Default::default()
    }), bollard::container::Config{

        ..Default::default()
    }).await?;

    Ok(())
}

async fn command_run_standalone(run_matches: &ArgMatches, standalone_matches: &ArgMatches) {
    let mut csharp = run_matches.get_flag("csharp");
    let mut java = run_matches.get_flag("java");
    let mut python = run_matches.get_flag("python");
    let mut rust = run_matches.get_flag("rust");
    let mut nodejs = run_matches.get_flag("nodejs");
    let mut go = run_matches.get_flag("go");
    if !csharp && !java && !python && !rust && !nodejs && !go {
        csharp = true;
        java = true;
        python = true;
        rust = true;
        nodejs = true;
        go = true;
    }
    todo!()
}
