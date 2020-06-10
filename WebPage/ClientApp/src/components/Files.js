import React, { Component } from 'react';
//import { useHistory } from 'react-router-dom';
import { Link } from 'react-router-dom';

export class Files extends Component {
  static displayName = Files.name;

  constructor(props) {
    super(props);
      this.state = {
          files: [],
          loading: true,
          filePath: "",
          user: ""
      };

      this.postFile = this.postFile.bind(this);
      this.changeFilePath = this.changeFilePath.bind(this);
      this.changeUser = this.changeUser.bind(this);
     // this.goToResults = this.goToResults.bind(this);
      this.handleChange = this.handleChange.bind(this);
  }

  componentDidMount() {
    this.FetchFiles();
    }


    goToResults(id) {
        console.log(id);
    }

    handleChange(e) {
        console.log(e.target.value);
    }

  renderFilesTable(files) {
    return (
      <table className='table table-striped' aria-labelledby="tabelLabel">
        <thead>
          <tr>
                    <th>fileId</th>
                    <th>filePath</th>
                    <th>fileDuration</th>
                    <th>fileType</th>
                    <th>jobId</th>
                    <th>jobType </th>
                    <th>jobCreated</th>
                    <th>jobUpdated</th>
                    <th>jobUser</th>
                    <th>Progress</th>
                    <th>jobDuration</th>
                    <th>jobFinished</th>
          </tr>
        </thead>
        <tbody>
                {files.map(f => 
                    <tr key={f.id}>
                        <td>{f.id}</td>
                        <td>{"...\\" + f.file_path.substring(f.file_path.lastIndexOf('\\') + 1)}</td>
                        <td>{f.file_duration}</td>
                        <td>{f.file_ext}</td>
                        <td>{f.jobId}</td>
                        <td>{f.job_type}</td>
                        <td nowrap>{f.created}</td>
                        <td nowrap>{f.last_updated}</td>
                        <td>{f.user}</td>
                        <td>{f.percentage}</td>
                        <td nowrap>{f.time_used}</td>
                        <td nowrap>{f.job_finished}</td>
                    </tr>
                )
                }
        </tbody>
      </table>
    );
    }

    
    changeFilePath(event) {
        this.setState({ filePath: event.target.value });
    }
    changeUser(event) {
        this.setState({ user: event.target.value });
    }

  render() {
    let contents = this.state.loading
      ? <p><em>Loading...</em></p>
      : this.renderFilesTable(this.state.files);

    return (
      <div>
            <h5 id="tabelLabel" >Files</h5>

            <form onSubmit={this.postFile}>
                <label>
                    File Path:
                <input type="text" name="File Path" onChange={this.changeFilePath} />
                </label>
                <br/>
                <label>
                    User:
                    <input type="text" name="User" onChange={this.changeUser}/>
                </label>

                <input type="submit" text="Post file for analysis" />
            </form> 

            <br/>
            {contents}
      </div>
    );
  }

    

    async postFile(e) {
        e.preventDefault();
        const input = { "audioPath": this.state.filePath, "user" : this.state.user };
        const settings = {
            method: "post",
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(input)
        };
        const url = "api/files/";

        const response = await fetch(url, settings);
        const data = await response.text();

        this.setState({ loading: false });
        this.FetchFiles();
        alert("A file was submitted for analysis: " + data);
    }

    async FetchFiles() {

        const response = await fetch('api/files');
        const data = await response.json();
        this.setState({ files: data, loading: false });
    }
}
